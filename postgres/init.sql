CREATE TABLE IF NOT EXISTS notification_logs (
    id SERIAL PRIMARY KEY,
    type TEXT NOT NULL,
    recipient TEXT,
    subject TEXT,
    message TEXT,
    status TEXT NOT NULL,
    error_message TEXT,
    created_at TIMESTAMP NOT NULL
);
