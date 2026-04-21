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

-- Таблица устройств, за которыми админ или статист хочет наблюдать.
CREATE TABLE IF NOT EXISTS monitored_devices (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    address VARCHAR(255) NULL,
    description TEXT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Таблица хранит историю снимков нагрузки CPU/RAM/HDD по устройствам.
CREATE TABLE IF NOT EXISTS system_metrics (
    id SERIAL PRIMARY KEY,
    device_id INT NOT NULL REFERENCES monitored_devices(id) ON DELETE CASCADE,
    cpu_percent NUMERIC(5,2) NOT NULL,
    ram_percent NUMERIC(5,2) NOT NULL,
    hdd_percent NUMERIC(5,2) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Индекс ускоряет вывод последних снимков нагрузки конкретного устройства.
CREATE INDEX IF NOT EXISTS idx_system_metrics_device_id_created_at
ON system_metrics(device_id, created_at DESC);
