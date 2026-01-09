# Notification Service — Cервис отправки уведомлений на разные источники

Цель проекта - разработать распределенный сервис отправки уведомлений через различные каналы (Email, Sms, Push и.т.д) использую микросервисную архитектуру, брокера сообщений, и централизованный мониторинг нашего сервиса.

## Описание проекта

Этот проект представляет собой микросервисную систему отправки уведомлений, реализованную на .NET 8 с использованием Kafka в качестве шины сообщений и PostgreSQL для логирования событий. Система разделена на несколько независимых сервисов, каждый из которых отвечает за обработку определённого типа уведомлений: SMS, Email и Push.

## Архитектура системы

Общая архитектура построена по событийной модели с использованием Kafka как брокера сообщений.

### Основные компоненты:

1. **Notification Gateway**
   - REST API на .NET 8 для приёма запросов на отправку уведомлений.
   - Публикует сообщения в Kafka-топик notifications.

2. **Notification Router**
   - Обрабатывает входящие сообщения из топика notifications.
   - По типу уведомления (Sms, Email, Push) распределяет их в соответствующие Kafka-топики:
       - notifications-sms
       - notifications-email
       - notifications-push
         
3. **Сервисы отправки уведомлений**
   - Email Service.
       - Подписан на notifications-email.
       - Отправляет письма через SMTP (Я использовал для тестирования сервис Mail.ru).
   - SMS Service.
       - Подписан на notifications-sms.
       - Отправляет SMS через внешний API (Использовал сервис Textbelt, но он поддерживает только зарубежные номера)
   - Push Service.
       - Подписан на notifications-push.
       - Не был подключен не к одному сервису, но работает в тестовом режиме.
   - Каждый из сервисов данные логирует в БД и поддерживает повторную отправку при ошибке

4. **Notification Status Service**
    - REST API для получения статуса уведомления по `notificationId`
    - Извлекает информацию из таблицы notification_logs в PostgreSQL.
    - Может использоваться другими системами для отображения истории доставки.
  
5. **Инфраструктура**
     - Kafka - Брокер сообщений, передача уведомлений между сервисами.
     - PostgreSQL - Хранение логов отправки уведомлений.
     - Prometheus - Сбор метрик.
     - Grafana - Визуализация метрик в виде дашбордов.
     - Docker Compose - Запуск всех сервисов и инфраструктуры.

## Локальный запуск

В корневой директории проекта выполнить:

```
docker-compose up -d --build
```
После успешного запуска будут доступны следующие сервисы:

Notification Gateway - http://localhost:5055/swagger

Kafka UI - http://localhost:8080

Prometheus - http://localhost:9090

Grafana - http://localhost:3000

Grafana:
  - Логин: admin
  - Пароль: admin

База Данных(PostgreSql)
  - Port - 5430
  - User - postgres
  - Password - postgres
  - Database - notifications

## Проверка работы сервиса (Insomnia)

Все уведомления отправляются через единый endpoint Gateway API:

```
POST http://localhost:5055/api/Notification
```

### Примеры типа уведомлений:

Email:

```
{
  "type": "Email",
  "recipient": "mylnikov2022@gmail.com",
  "subject": "Тестовое сообщение",
  "message": "Если ты это читаешь - значит все работает"
}
```
<img width="940" height="446" alt="image" src="https://github.com/user-attachments/assets/6818ae6d-d2f8-441b-b490-31a799367fce" />

Для отправки сообщений через свою почту нужно зайти в EmailService/appsettings.json 

Ввести данные в этих полях:

```
{
  "Host": "smtp.mail.ru",
  "Port": 587,
  "Username": "your_email@mail.ru", 
  "Password": "your_password", 
  "From": "your_email@mail.ru" 
}

```
Sms:

```
{
  "type": "Sms",
  "recipient": "+79824996535",
  "subject": "Тестовое сообщение",
  "message": "Проверь сообщение"
}
```

Push:

```
{
  "type": "Push",
	"recipient": "test-push",
  "subject": "Тестовое сообщение",
  "message": "Проверь Пуш!"
}
```
### Работа с Kafka-UI и Grafana:

При переходе в Kafka-UI можно увидеть все наши каналы:
<img width="1892" height="631" alt="image" src="https://github.com/user-attachments/assets/a50e4355-fcc6-4449-ba40-e66d716e6dda" />

<img width="1897" height="465" alt="image" src="https://github.com/user-attachments/assets/c67d2490-2fc3-416e-b103-d27cc578a244" />

И сообщения на примере Email:
<img width="1864" height="630" alt="image" src="https://github.com/user-attachments/assets/4c96b849-ff5d-4063-86ba-e889d5230967" />

После авторизации в Grafana заходим в папку Notifications

<img width="1911" height="573" alt="image" src="https://github.com/user-attachments/assets/bf37b168-12d8-485f-9e96-5e024a4a26db" />

И тут мы можем увидеть все наши дашборды

<img width="1893" height="936" alt="image" src="https://github.com/user-attachments/assets/6180a633-aa59-4e19-a3d1-e8f8cfcad479" />









   
