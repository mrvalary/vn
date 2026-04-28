-- Скрипт первичной подготовки базы данных vn.
-- Его можно выполнять повторно: таблицы, роли и недостающая колонка роли не будут созданы второй раз.

-- Таблица ролей хранит права пользователей на уровне данных.
CREATE TABLE IF NOT EXISTS roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL
);

-- Базовые роли приложения.
INSERT INTO roles (name, display_name)
VALUES
    ('user', 'Пользователь'),
    ('admin', 'Админ'),
    ('statistician', 'Статист')
ON CONFLICT (name) DO UPDATE
SET display_name = EXCLUDED.display_name;

-- Таблица пользователей.
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    login VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role_id INT,
    is_blocked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Для старых баз, где таблица users уже была создана без role_id.
ALTER TABLE users
ADD COLUMN IF NOT EXISTS role_id INT;

-- Для старых баз добавляем признак блокировки пользователя.
ALTER TABLE users
ADD COLUMN IF NOT EXISTS is_blocked BOOLEAN NOT NULL DEFAULT FALSE;

-- Для старых баз добавляем дату создания пользователя.
ALTER TABLE users
ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT NOW();

-- Старым пользователям без роли назначается обычный пользователь.
UPDATE users
SET role_id = (SELECT id FROM roles WHERE name = 'user')
WHERE role_id IS NULL;

-- После заполнения старых пользователей роль становится обязательной.
ALTER TABLE users
ALTER COLUMN role_id SET NOT NULL;

-- Добавляем внешний ключ на roles, если его ещё нет.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_users_roles'
    ) THEN
        ALTER TABLE users
        ADD CONSTRAINT fk_users_roles
        FOREIGN KEY (role_id)
        REFERENCES roles(id);
    END IF;
END $$;

-- Таблица заметок пользователя.
CREATE TABLE IF NOT EXISTS notes (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    note_text TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Индекс ускоряет получение заметок конкретного пользователя.
CREATE INDEX IF NOT EXISTS idx_notes_user_id ON notes(user_id);

-- Индекс ускоряет выборку пользователей по роли.
CREATE INDEX IF NOT EXISTS idx_users_role_id ON users(role_id);

-- Журнал безопасности хранит важные события входа и администрирования.
CREATE TABLE IF NOT EXISTS security_logs (
    id SERIAL PRIMARY KEY,
    actor_user_id INT NULL REFERENCES users(id) ON DELETE SET NULL,
    actor_login VARCHAR(255) NULL,
    event_type VARCHAR(100) NOT NULL,
    message TEXT NOT NULL,
    target VARCHAR(255) NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Индекс ускоряет просмотр последних событий.
CREATE INDEX IF NOT EXISTS idx_security_logs_created_at ON security_logs(created_at DESC);

-- Индекс ускоряет фильтрацию по типу события в будущих командах.
CREATE INDEX IF NOT EXISTS idx_security_logs_event_type ON security_logs(event_type);

-- Устройства, с которых Watcher отправляет метрики.
CREATE TABLE IF NOT EXISTS monitored_devices (
    id SERIAL PRIMARY KEY,
    device_key VARCHAR(100) NULL,
    name VARCHAR(100) NOT NULL,
    address VARCHAR(255) NULL,
    description TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_seen_at TIMESTAMP NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

ALTER TABLE monitored_devices
ADD COLUMN IF NOT EXISTS device_key VARCHAR(100);

ALTER TABLE monitored_devices
ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;

ALTER TABLE monitored_devices
ADD COLUMN IF NOT EXISTS last_seen_at TIMESTAMP NULL;

UPDATE monitored_devices
SET device_key = name
WHERE device_key IS NULL OR device_key = '';

ALTER TABLE monitored_devices
ALTER COLUMN device_key SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_monitored_devices_device_key
ON monitored_devices(device_key);

-- История снимков нагрузки CPU/RAM/HDD, которую присылают Watcher-агенты.
CREATE TABLE IF NOT EXISTS system_metrics (
    id SERIAL PRIMARY KEY,
    device_id INT NOT NULL REFERENCES monitored_devices(id) ON DELETE CASCADE,
    cpu_percent NUMERIC(5,2) NOT NULL,
    ram_percent NUMERIC(5,2) NOT NULL,
    hdd_percent NUMERIC(5,2) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_system_metrics_device_id_created_at
ON system_metrics(device_id, created_at DESC);

-- Database roles used by the application.
-- vn_app_auth is the startup account from App.config. It is not a PostgreSQL superuser.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'vn_app_auth') THEN
        CREATE ROLE vn_app_auth LOGIN PASSWORD 'vn_app_auth_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'vn_user_role') THEN
        CREATE ROLE vn_user_role LOGIN PASSWORD 'vn_user_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'vn_admin_role') THEN
        CREATE ROLE vn_admin_role LOGIN PASSWORD 'vn_admin_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'vn_statistician_role') THEN
        CREATE ROLE vn_statistician_role LOGIN PASSWORD 'vn_statistician_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'vn_watcher') THEN
        CREATE ROLE vn_watcher LOGIN PASSWORD 'vn_watcher_password';
    END IF;
END $$;

ALTER ROLE vn_app_auth LOGIN PASSWORD 'vn_app_auth_password';
ALTER ROLE vn_user_role LOGIN PASSWORD 'vn_user_password';
ALTER ROLE vn_admin_role LOGIN PASSWORD 'vn_admin_password';
ALTER ROLE vn_statistician_role LOGIN PASSWORD 'vn_statistician_password';
ALTER ROLE vn_watcher LOGIN PASSWORD 'vn_watcher_password';

ALTER TABLE roles
ADD COLUMN IF NOT EXISTS connection_string TEXT;

-- В таблице ролей храним только учетные данные роли.
-- Host, Port и Database приложение всегда берет из NotesDb в App.config.
UPDATE roles
SET connection_string = 'Username=vn_user_role;Password=vn_user_password'
WHERE name = 'user';

UPDATE roles
SET connection_string = 'Username=vn_admin_role;Password=vn_admin_password'
WHERE name = 'admin';

UPDATE roles
SET connection_string = 'Username=vn_statistician_role;Password=vn_statistician_password'
WHERE name = 'statistician';

CREATE OR REPLACE FUNCTION get_role_connection_string(p_role_name VARCHAR)
RETURNS TEXT
LANGUAGE sql
SECURITY DEFINER
SET search_path = public
AS $$
    SELECT connection_string
    FROM roles
    WHERE name = p_role_name;
$$;

CREATE OR REPLACE FUNCTION save_device_metric(
    p_device_key VARCHAR,
    p_device_name VARCHAR,
    p_cpu_percent NUMERIC,
    p_ram_percent NUMERIC,
    p_hdd_percent NUMERIC
)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_device_id INT;
BEGIN
    IF p_device_key IS NULL OR btrim(p_device_key) = '' THEN
        RAISE EXCEPTION 'device_key is required';
    END IF;

    INSERT INTO monitored_devices (device_key, name, last_seen_at, is_active)
    VALUES (
        btrim(p_device_key),
        COALESCE(NULLIF(btrim(p_device_name), ''), btrim(p_device_key)),
        NOW(),
        TRUE
    )
    ON CONFLICT (device_key) DO UPDATE
    SET name = COALESCE(NULLIF(EXCLUDED.name, ''), monitored_devices.name),
        last_seen_at = NOW()
    RETURNING id INTO v_device_id;

    INSERT INTO system_metrics (device_id, cpu_percent, ram_percent, hdd_percent)
    VALUES (
        v_device_id,
        LEAST(GREATEST(COALESCE(p_cpu_percent, 0), 0), 100),
        LEAST(GREATEST(COALESCE(p_ram_percent, 0), 0), 100),
        LEAST(GREATEST(COALESCE(p_hdd_percent, 0), 0), 100)
    );
END;
$$;

REVOKE ALL ON FUNCTION get_role_connection_string(VARCHAR) FROM PUBLIC;
REVOKE ALL ON FUNCTION save_device_metric(VARCHAR, VARCHAR, NUMERIC, NUMERIC, NUMERIC) FROM PUBLIC;

REVOKE ALL ON ALL TABLES IN SCHEMA public FROM vn_app_auth;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM vn_app_auth;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM vn_statistician_role;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM vn_statistician_role;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM vn_watcher;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM vn_watcher;

GRANT USAGE ON SCHEMA public TO vn_app_auth, vn_user_role, vn_admin_role, vn_statistician_role, vn_watcher;
GRANT EXECUTE ON FUNCTION get_role_connection_string(VARCHAR) TO vn_app_auth, vn_user_role, vn_admin_role, vn_statistician_role;
GRANT EXECUTE ON FUNCTION save_device_metric(VARCHAR, VARCHAR, NUMERIC, NUMERIC, NUMERIC) TO vn_watcher;

GRANT SELECT ON users, roles TO vn_app_auth;
GRANT INSERT ON users, security_logs TO vn_app_auth;
GRANT USAGE, SELECT ON SEQUENCE users_id_seq, security_logs_id_seq TO vn_app_auth;

GRANT SELECT ON users, roles TO vn_user_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON notes TO vn_user_role;
GRANT INSERT ON security_logs TO vn_user_role;
GRANT USAGE, SELECT ON SEQUENCE notes_id_seq, security_logs_id_seq TO vn_user_role;

GRANT SELECT, INSERT, UPDATE, DELETE ON users, roles, notes, security_logs, monitored_devices, system_metrics TO vn_admin_role;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO vn_admin_role;

GRANT SELECT ON monitored_devices, system_metrics TO vn_statistician_role;
GRANT INSERT, UPDATE, DELETE ON monitored_devices TO vn_statistician_role;
GRANT INSERT ON security_logs TO vn_statistician_role;
GRANT USAGE, SELECT ON SEQUENCE monitored_devices_id_seq, security_logs_id_seq TO vn_statistician_role;
