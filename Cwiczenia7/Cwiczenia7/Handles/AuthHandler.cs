using Cwiczenia7.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Cwiczenia7.Handles
{
    public class AuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public AuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
            ) : base(options, logger, encoder, clock)
        {

        }
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Missing authorization header");
            var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
            var credentialsBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialsBytes).Split(":");

            if (credentials.Length != 2)
                return AuthenticateResult.Fail("Incorrect authorization header value");
            Student student = new Student();
            var connection = new SqlConnection("Data Source=db-mssql;Initial Catalog=s19541;Integrated Security=True");
            connection.Open();
            using (var com = new SqlCommand())
            {
                com.Connection = connection;
                com.CommandText = "select * from Student where IndexNumber=@login AND Password=@password";
                com.Parameters.AddWithValue("login", credentials[0]);
                com.Parameters.AddWithValue("password", credentials[1]);

                var dr = com.ExecuteReader();
                
                while (dr.Read())
                {
                    student.IndexNumber = Int32.Parse(dr["IndexNumber"].ToString());
                    student.FirstName = dr["FirstName"].ToString();
                    student.LastName = dr["LastName"].ToString(); 
                    student.Password = dr["Password"].ToString();
                }
             

            }
            connection.Close();
            var claims = new[]
           {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, credentials[0]),
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name); 
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            if (student.FirstName != null)
                return AuthenticateResult.Success(ticket);
            return AuthenticateResult.Fail("Bad Login or Password");
        }
    }
}
