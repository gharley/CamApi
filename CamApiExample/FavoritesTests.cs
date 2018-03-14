using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using CamApi;

namespace CamApiExample
{
    class FavoritesTests
    {
        private CamApiLib api;

        public FavoritesTests(CamApiLib api)
        {
            this.api = api;
        }

        public void DeleteAllFavorites()
        {
            var ids = GetFavoriteIds();

            foreach (var id in ids)
            {
                var result = DeleteFavorite(id);

                Console.WriteLine($"result after delete: {result}");
            }
        }

        public CAMAPI_STATUS DeleteFavorite(string id)
        {
            // Deletes a previously saved favorite using id to identify which favorite to delete
            // returns: CAMAPI status
            string jdata = api.FetchTarget($"/delete_favorite?id={id}");

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }

        public CamDictionary GetFavorite(string id)
        {
            // returns: dictionary containing previously saved favorite settings with identifier id.
            string jdata = api.FetchTarget($"/get_favorite?id={id}");

            return (CamDictionary)JsonConvert.DeserializeObject(jdata, typeof(CamDictionary));
        }

        public List<string> GetFavoriteIds()
        {
            // returns: list of saved favorite setting identifiers, will be an empty list if no settings have been saved.
            var jdata = api.FetchTarget("/get_favorite_ids");

            return (List<string>)JsonConvert.DeserializeObject(jdata, typeof(List<string>));
        }

        public void Run()
        {
            DeleteAllFavorites();
            Console.WriteLine($"Number of favorite IDs (should be 0): {GetFavoriteIds().Count}");

            var settings = api.GetCurrentSettings();

            settings["id"] = 5;
            settings["duration"] = 2;
            settings["notes"] = "Favorite settings stored in ID slot 5";

            SaveFavorite(settings);
            Console.WriteLine("Saved settings in ID slot 5");

            settings = api.GetCurrentSettings();

            settings["id"] = 3;
            settings["duration"] = 4;
            settings["notes"] = "Favorite settings stored in ID slot 3";

            SaveFavorite(settings);
            Console.WriteLine("Saved settings in ID slot 3");

            Console.WriteLine($"Saved favorite ID list: {string.Join(", ", GetFavoriteIds())}");

            settings = GetFavorite("5");
            var allowedSettings = api.ConfigureCamera(settings);

            api.Run(allowedSettings);
            Console.WriteLine("Retreived favorite settings 5 and configured the camera using those settings");

            DeleteFavorite("3");
            Console.WriteLine("Deleted previously save favorite in ID slot 3");
            Console.WriteLine($"Saved favorite ID list: {string.Join(", ", GetFavoriteIds())}");
        }

        public CAMAPI_STATUS SaveFavorite(CamDictionary settings)
        {
            // Save a set of camera settings.  The supplied settings must have an id key set to a valid value.
            // returns: CAMAPI_STATUS.OKAY, CAMAPI_STATUS.INVALID_PARAMETER, or CAMAPI_STATUS.STORAGE_ERROR
            string jdata = api.PostTarget("/save_favorite", settings);

            return (CAMAPI_STATUS)JsonConvert.DeserializeObject(jdata, typeof(CAMAPI_STATUS));
        }
    }
}