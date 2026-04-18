using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using Movie_Catalog_Exam.Models;

namespace Movie_Catalog_Exam
{
    [TestFixture]
    public class Tests()
    {
        private RestClient client;

        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIyN2JhZDYxZS0yNjkwLTRjMWMtYWNkNC03NDg3ZGQxY2RiMTkiLCJpYXQiOiIwNC8xOC8yMDI2IDA3OjMxOjMzIiwiVXNlcklkIjoiOTRhNGVjN2MtYTFjMi00MTk4LTYyZTUtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJRQUlyaW5hU0BleGFtcGxlLmNvbSIsIlVzZXJOYW1lIjoiUUFJcmluYVMiLCJleHAiOjE3NzY1MTkwOTMsImlzcyI6Ik1vdmllQ2F0YWxvZ19BcHBfU29mdFVuaSIsImF1ZCI6Ik1vdmllQ2F0YWxvZ19XZWJBUElfU29mdFVuaSJ9.k5QtYnAk-l5Nkn5ZDPgL9XDC2B-2sDl5AM0Lk5AHrFw";

        private const string LoginEmail = "QAIrinaS@example.com";
        private const string LoginPassword = "123Irina";

        private static string CreatedMovieId = string.Empty;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateMovie_WithRequiredFIelds_ShouldReturnSuccess()
        {
            var movieData = new MovieDTO
            {
                Id = "1234",
                Title = "Test movie",
                Description = "This is a test movie description.",

            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            var response = this.client.Execute(request);

            var deserializedResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(deserializedResponse.Msg, Is.EqualTo("Movie created successfully!"));
            Assert.That(deserializedResponse.Movie, Is.Not.Null);
            Assert.That(deserializedResponse.Movie, Is.TypeOf<MovieDTO>());
            Assert.That(deserializedResponse.Movie.Id, Is.Not.Null.Or.Empty);

            CreatedMovieId = deserializedResponse.Movie.Id;
        }

        [Order(2)]
        [Test]

        public void EditMovie_ThatWasCreated_ShouldReturnSuccess()
        {
            var editRequestData = new MovieDTO
            {
                Id = "4321",
                Title = "Edited Movie",
                Description = "This is an edited movie description.",
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);

            request.AddQueryParameter("movieid", CreatedMovieId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseItems, Is.Not.Empty);
        }

        [Order(4)]
        [Test]
        public void DeleteMovie_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", CreatedMovieId);
            var response = this.client.Execute(request);

            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(deleteResponse.Msg , Is.EqualTo("Movie deleted successfully!"));
        }


        [Order(5)]
        [Test]
        public void CreateMovie_WithoutRequiredFIelds_ShouldReturnBadRequst()
        {
            var movieData = new MovieDTO
            {
                Id = "5678"
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            var response = this.client.Execute(request);

            var deserializedResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(6)]
        [Test]
        public void EditNonExistingMovie_ShouldReturnNotFound()
        {
            string nonExistingMovieID = "00000";
            var editRequestData = new MovieDTO
            {
                Id = nonExistingMovieID,
                Title = "Edited Movie",
                Description = "This is an edited movie description.",
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);

            request.AddQueryParameter("movieid", nonExistingMovieID);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            var deserializedResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(deserializedResponse.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!")); 
        }
        
        [Order(7)]
        [Test]
        public void DeleteNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieID = "00000";
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExistingMovieID);
            var response = this.client.Execute(request);

            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(deleteResponse.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }


        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}