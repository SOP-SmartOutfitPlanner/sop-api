using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;

namespace SOPServer.API.Configurations
{
    public static class FirebaseAppConfiguration
    {
        public static void AddFirebaseAppConfiguration(
            IWebHostEnvironment environment)
        {
            var pathFirebase = string.Empty;

            if(!environment.IsDevelopment())
            {
                pathFirebase = Environment.GetEnvironmentVariable("PATH_FIREBASE_ADMIN");
            }
            else
            {
                pathFirebase = "firebase-adminsdk.json";
            }

            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(pathFirebase)
            });
        }
    }
}
