-- Полный скрипт подготовки базы данных vn для курсового проекта.
-- Выполнять под администратором PostgreSQL, подключившись к базе vn.
--
-- Если базу нужно создать заново, сначала выполните отдельно:
--   DROP DATABASE IF EXISTS vn;
--   CREATE DATABASE vn WITH ENCODING 'UTF8';
-- Затем подключитесь к базе vn и выполните этот файл целиком.

BEGIN;

-- =========================================================
-- 1. Роли PostgreSQL, под которыми работает приложение
-- =========================================================

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

-- =========================================================
-- 2. Справочник ролей приложения
-- =========================================================

CREATE TABLE IF NOT EXISTS roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    connection_string TEXT NULL
);

INSERT INTO roles (name, display_name, connection_string)
VALUES
    ('user', 'Пользователь', 'Username=vn_user_role;Password=vn_user_password'),
    ('admin', 'Админ', 'Username=vn_admin_role;Password=vn_admin_password'),
    ('statistician', 'Статист', 'Username=vn_statistician_role;Password=vn_statistician_password')
ON CONFLICT (name) DO UPDATE
SET display_name = EXCLUDED.display_name,
    connection_string = EXCLUDED.connection_string;

-- =========================================================
-- 3. Пользователи
-- =========================================================

CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    login VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role_id INT NOT NULL REFERENCES roles(id),
    is_blocked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_users_role_id ON users(role_id);

-- Стартовый администратор для свежей базы.
-- Логин: mrvalary
-- Пароль: mrvalary
-- После первой проверки пароль лучше сменить или удалить эту учетную запись.
INSERT INTO users (login, password_hash, role_id, is_blocked)
VALUES (
    'mrvalary',
    md5('mrvalary'),
    (SELECT id FROM roles WHERE name = 'admin'),
    FALSE
)
ON CONFLICT (login) DO NOTHING;

-- =========================================================
-- 4. Заметки пользователей
-- =========================================================

CREATE TABLE IF NOT EXISTS notes (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    note_text TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_notes_user_id ON notes(user_id);
CREATE INDEX IF NOT EXISTS idx_notes_user_id_created_at ON notes(user_id, created_at DESC, id DESC);

-- =========================================================
-- 5. Журнал безопасности
-- =========================================================

CREATE TABLE IF NOT EXISTS security_logs (
    id SERIAL PRIMARY KEY,
    actor_user_id INT NULL REFERENCES users(id) ON DELETE SET NULL,
    actor_login VARCHAR(255) NULL,
    event_type VARCHAR(100) NOT NULL,
    message TEXT NOT NULL,
    target VARCHAR(255) NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_security_logs_created_at ON security_logs(created_at DESC, id DESC);
CREATE INDEX IF NOT EXISTS idx_security_logs_event_type ON security_logs(event_type);
CREATE INDEX IF NOT EXISTS idx_security_logs_actor_user_id ON security_logs(actor_user_id);

-- =========================================================
-- 6. Устройства мониторинга
-- =========================================================

CREATE TABLE IF NOT EXISTS monitored_devices (
    id SERIAL PRIMARY KEY,
    device_key VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    address VARCHAR(255) NULL,
    description TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_seen_at TIMESTAMP NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_monitored_devices_name ON monitored_devices(name);
CREATE INDEX IF NOT EXISTS idx_monitored_devices_is_active ON monitored_devices(is_active);

-- =========================================================
-- 7. История метрик CPU/RAM/HDD
-- =========================================================

CREATE TABLE IF NOT EXISTS system_metrics (
    id SERIAL PRIMARY KEY,
    device_id INT NOT NULL REFERENCES monitored_devices(id) ON DELETE CASCADE,
    cpu_percent NUMERIC(5,2) NOT NULL,
    ram_percent NUMERIC(5,2) NOT NULL,
    hdd_percent NUMERIC(5,2) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

ALTER TABLE system_metrics
DROP CONSTRAINT IF EXISTS chk_system_metrics_cpu_percent;

ALTER TABLE system_metrics
ADD CONSTRAINT chk_system_metrics_cpu_percent
CHECK (cpu_percent >= 0 AND cpu_percent <= 100);

ALTER TABLE system_metrics
DROP CONSTRAINT IF EXISTS chk_system_metrics_ram_percent;

ALTER TABLE system_metrics
ADD CONSTRAINT chk_system_metrics_ram_percent
CHECK (ram_percent >= 0 AND ram_percent <= 100);

ALTER TABLE system_metrics
DROP CONSTRAINT IF EXISTS chk_system_metrics_hdd_percent;

ALTER TABLE system_metrics
ADD CONSTRAINT chk_system_metrics_hdd_percent
CHECK (hdd_percent >= 0 AND hdd_percent <= 100);

CREATE INDEX IF NOT EXISTS idx_system_metrics_device_id_created_at
ON system_metrics(device_id, created_at DESC, id DESC);

-- =========================================================
-- 8. Функции, которые вызываются из C#
-- =========================================================

CREATE OR REPLACE FUNCTION get_role_connection_string(p_role_name VARCHAR)
RETURNS TEXT
LANGUAGE sql
SECURITY DEFINER
SET search_path = public
AS '
    SELECT connection_string
    FROM roles
    WHERE name = p_role_name
';

CREATE OR REPLACE FUNCTION save_device_metric(
    p_device_key VARCHAR,
    p_device_name VARCHAR,
    p_cpu_percent NUMERIC,
    p_ram_percent NUMERIC,
    p_hdd_percent NUMERIC
)
RETURNS VOID
LANGUAGE sql
SECURITY DEFINER
SET search_path = public
AS '
    WITH input_values AS (
        SELECT
            NULLIF(btrim(p_device_key), '''') AS device_key,
            NULLIF(btrim(p_device_name), '''') AS device_name,
            LEAST(GREATEST(COALESCE(p_cpu_percent, 0), 0), 100) AS cpu_percent,
            LEAST(GREATEST(COALESCE(p_ram_percent, 0), 0), 100) AS ram_percent,
            LEAST(GREATEST(COALESCE(p_hdd_percent, 0), 0), 100) AS hdd_percent
    ),
    upserted_device AS (
        INSERT INTO monitored_devices (device_key, name, last_seen_at, is_active)
        SELECT
            device_key,
            COALESCE(device_name, device_key),
            NOW(),
            TRUE
        FROM input_values
        WHERE device_key IS NOT NULL
        ON CONFLICT (device_key) DO UPDATE
        SET name = COALESCE(EXCLUDED.name, monitored_devices.name),
            last_seen_at = NOW(),
            is_active = TRUE
        RETURNING id
    )
    INSERT INTO system_metrics (device_id, cpu_percent, ram_percent, hdd_percent)
    SELECT
        upserted_device.id,
        input_values.cpu_percent,
        input_values.ram_percent,
        input_values.hdd_percent
    FROM upserted_device
    CROSS JOIN input_values
';

-- =========================================================
-- 9. Безопасность и права доступа
-- =========================================================

REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO vn_app_auth, vn_user_role, vn_admin_role, vn_statistician_role, vn_watcher;

REVOKE ALL ON ALL TABLES IN SCHEMA public FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM vn_app_auth, vn_user_role, vn_admin_role, vn_statistician_role, vn_watcher;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM vn_app_auth, vn_user_role, vn_admin_role, vn_statistician_role, vn_watcher;
REVOKE ALL ON FUNCTION get_role_connection_string(VARCHAR) FROM PUBLIC;
REVOKE ALL ON FUNCTION save_device_metric(VARCHAR, VARCHAR, NUMERIC, NUMERIC, NUMERIC) FROM PUBLIC;
REVOKE ALL ON FUNCTION get_role_connection_string(VARCHAR) FROM vn_app_auth, vn_user_role, vn_admin_role, vn_statistician_role, vn_watcher;
REVOKE ALL ON FUNCTION save_device_metric(VARCHAR, VARCHAR, NUMERIC, NUMERIC, NUMERIC) FROM vn_app_auth, vn_user_role, vn_admin_role, vn_statistician_role, vn_watcher;

-- Стартовая роль из App.config.
-- Она может зарегистрировать пользователя, проверить логин/пароль и получить строку роли через функцию.
GRANT SELECT (id, name, display_name) ON roles TO vn_app_auth;
GRANT SELECT (id, login, password_hash, role_id, is_blocked, created_at) ON users TO vn_app_auth;
GRANT INSERT (login, password_hash, role_id, is_blocked) ON users TO vn_app_auth;
GRANT INSERT (actor_user_id, actor_login, event_type, message, target) ON security_logs TO vn_app_auth;
GRANT USAGE, SELECT ON SEQUENCE users_id_seq, security_logs_id_seq TO vn_app_auth;
GRANT EXECUTE ON FUNCTION get_role_connection_string(VARCHAR) TO vn_app_auth;

-- Обычный пользователь.
-- Может работать со своими заметками. Ограничение "только свои" контролируется SQL-запросами приложения.
GRANT SELECT (id, name, display_name) ON roles TO vn_user_role;
GRANT SELECT (id, login, role_id, is_blocked, created_at) ON users TO vn_user_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON notes TO vn_user_role;
GRANT INSERT (actor_user_id, actor_login, event_type, message, target) ON security_logs TO vn_user_role;
GRANT USAGE, SELECT ON SEQUENCE notes_id_seq, security_logs_id_seq TO vn_user_role;
GRANT EXECUTE ON FUNCTION get_role_connection_string(VARCHAR) TO vn_user_role;

-- Администратор.
-- Может управлять пользователями, заметками, логами и мониторингом.
GRANT SELECT (id, name, display_name) ON roles TO vn_admin_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON users TO vn_admin_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON notes TO vn_admin_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON security_logs TO vn_admin_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON monitored_devices TO vn_admin_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON system_metrics TO vn_admin_role;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO vn_admin_role;
GRANT EXECUTE ON FUNCTION get_role_connection_string(VARCHAR) TO vn_admin_role;

-- Статист.
-- Может смотреть мониторинг, редактировать карточки устройств и смотреть журнал безопасности.
GRANT SELECT (id, name, display_name) ON roles TO vn_statistician_role;
GRANT SELECT (id, login, role_id, is_blocked, created_at) ON users TO vn_statistician_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON monitored_devices TO vn_statistician_role;
GRANT SELECT ON system_metrics TO vn_statistician_role;
GRANT SELECT, INSERT ON security_logs TO vn_statistician_role;
GRANT USAGE, SELECT ON SEQUENCE monitored_devices_id_seq, security_logs_id_seq TO vn_statistician_role;
GRANT EXECUTE ON FUNCTION get_role_connection_string(VARCHAR) TO vn_statistician_role;

-- Watcher-agent.
-- У него нет прямых прав на таблицы. Он может только вызвать функцию save_device_metric.
GRANT EXECUTE ON FUNCTION save_device_metric(VARCHAR, VARCHAR, NUMERIC, NUMERIC, NUMERIC) TO vn_watcher;

COMMIT;
