using NotificationGateway.Kafka;
using Prometheus; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<KafkaNotificationProducer>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpMetrics();   

// app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapMetrics();       

app.Run();