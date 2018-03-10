using System;

using CamApi;

namespace CamApiExample
{
    class FavoritesTests
    {
        private CamApiLib api;

        public FavoritesTests(CamApiLib api){
            this.api = api;
        }

        public void Run()
        {
            api.DeleteAllFavorites();
            Console.WriteLine($"Number of favorite IDs (should be 0): {api.GetFavoriteIds().Count}");

            var settings = api.GetCurrentSettings();

            settings["id"] = 5;
            settings["duration"] = 2;
            settings["notes"] = "Favorite settings stored in ID slot 5";

            api.SaveFavorite(settings);
            Console.WriteLine("Saved settings in ID slot 5");

            settings = api.GetCurrentSettings();

            settings["id"] = 3;
            settings["duration"] = 4;
            settings["notes"] = "Favorite settings stored in ID slot 3";

            api.SaveFavorite(settings);
            Console.WriteLine("Saved settings in ID slot 3");

            Console.WriteLine($"Saved favorite ID list: {string.Join(", ", api.GetFavoriteIds())}");

            settings = api.GetFavorite("5");
            var allowedSettings = api.ConfigureCamera(settings);

            api.Run(allowedSettings);
            Console.WriteLine("Retreived favorite settings 5 and configured the camera using those settings");

            api.DeleteFavorite("3");
            Console.WriteLine("Deleted previously save favorite in ID slot 3");
            Console.WriteLine($"Saved favorite ID list: {string.Join(", ", api.GetFavoriteIds())}");
        }
    }
}