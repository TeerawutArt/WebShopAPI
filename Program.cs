using System.Text;
using WebShoppingAPI;
using WebShoppingAPI.Filters;
using WebShoppingAPI.Helpers;
using WebShoppingAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);



///////service section////////
var services = builder.Services;
//add service//
services.AddScoped<FileService>();
services.AddScoped<TokenHelper>();
services.AddScoped<PriceCalculateService>();
services.AddHostedService<CheckTimeEventService>(); //AddHostedService ทำงานพื้นหลังตลอดเวลา (ไม่ต้องเรียกใช้งาน)
//controller//
services.AddControllers();

//////database section//////
services.AddDbContext<AppDbContext>(options =>
{
    //install sqllite packet ด้วย
    options.UseSqlite(builder.Configuration.GetConnectionString("sqlite"));
});
services.AddIdentity<UserModel, RoleModel>(options =>
{
    options.User.RequireUniqueEmail = true;
    //ไม่เอาตัวอักษรพิเศษ
    options.Password.RequireNonAlphanumeric = false;


}).AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

///////////cross-origin/////////////////
services.AddCors(options =>
{
    options.AddPolicy("MyCors", config =>
    {
        config
        .WithOrigins(builder.Configuration.GetSection("AllowedOrigins")
        .Get<string[]>()!)
        .AllowAnyMethod().AllowAnyHeader();
    });
});
///////////////////////////////////////////
///////////////JWT token//////////////////
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = jwtSettings["ValidIssuer"],
        ValidAudience = jwtSettings["ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecurityKey"]!))
    };
});
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

//app build//
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
//hosting image path ประมาณว่า //localhost:port/requestPath/fileName
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "UploadImage")),
    RequestPath = "/Images"
});
app.UseCors("MyCors");
// add authentication middleware
app.UseAuthentication();
// add authorization middleware
app.UseAuthorization();
//endpoint
app.MapControllers();
//run
app.Run();
