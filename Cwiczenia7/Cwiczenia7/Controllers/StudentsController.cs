using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Cwiczenia7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cwiczenia7.Controllers
{
    [Route("api/students")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        public IConfiguration Configuration { get; set; }
        public StudentsController(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        [HttpGet]
        [Authorize(Roles = "employee")]
        public IActionResult GetStudents()
        {
            
            var list = new List<Student>();
            list.Add(new Student
            {
                IndexNumber = 1,
                FirstName = "Jan",
                LastName = "Kowalski"
            });
            list.Add(new Student
            {
                IndexNumber = 2,
                FirstName = "Jakub",
                LastName = "Nowak"
            });

            return Ok(list);
        }
        [HttpPost]
        public IActionResult Login(DTOs.LoginRequestDto request)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "jan123"),
                new Claim(ClaimTypes.Role, "employee")
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken
            (
                issuer: "Gakko",
                audience: "Students",
                claims: claims,
                expires: DateTime.Now.AddMinutes(10),
                signingCredentials: creds
            );

            Student student = new Student();
            var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=s19541;Integrated Security=True");
            connection.Open();
            using (var com = new SqlCommand())
            {
                com.Connection = connection;
                com.CommandText = "select * from Student where IndexNumber=@login";
                com.Parameters.AddWithValue("login", request.Login);

                var dr = com.ExecuteReader();

                if (!dr.HasRows)
                    return BadRequest("Wrong login(index)");


            }
            connection.Close();

            var pass = Configuration["Password"+request.Login];
            if (request.Password != pass)
                return BadRequest("Wrong password");

            var refreshToken = Guid.NewGuid();
            Configuration["RefreshToken"] = refreshToken.ToString();
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken = refreshToken
            });
            
        }
        [HttpPost("refresh-token/{refreshToken}")]
        public IActionResult CreateNewToken(string refreshToken)
        {
            if (Configuration["RefreshToken"] != refreshToken)
                return BadRequest("Wrong refreshToken"+ Configuration["RefreshToken"]+"/"+refreshToken);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "jan123"),
                new Claim(ClaimTypes.Role, "employee")
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken
            (
                issuer: "Gakko",
                audience: "Students",
                claims: claims,
                expires: DateTime.Now.AddMinutes(10),
                signingCredentials: creds
            );

            var refreshTokenNew = Guid.NewGuid();
            Configuration["RefreshToken"] = refreshTokenNew.ToString();
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken = refreshTokenNew
            });
        }
    }
}