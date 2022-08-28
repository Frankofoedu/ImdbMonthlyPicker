using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImdbMonthlyPicker.Pages
{
    public class Top250Data
    {
        public List<Top250DataDetail> Items { get; set; }

        public string ErrorMessage { get; set; }
    }

    public class Top250DataDetail
    {
        public bool IsWatched { get; set; }
        public string Id { get; set; }
        public string Rank { get; set; }
        public string Title { set; get; }
        public string FullTitle { set; get; }
        public string Year { set; get; }
        public string Image { get; set; }
        public string Crew { get; set; }
        public string IMDbRating { get; set; }
        public string IMDbRatingCount { get; set; }
    }

    public class IndexModel : PageModel
    {
        private const string TOP_250_URL = "https://imdb-api.com/en/API/Top250Movies/";
        private static string API_KEY = "";
        private const string FILE_NAME = "Movies.json";

        private static readonly JsonSerializerOptions _options =
       new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        private static readonly Random Rand = new();

        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration;

        public Top250DataDetail MovieDetail { get; set; } = new Top250DataDetail();
        public string ErrorMessage;

        public IndexModel(ILogger<IndexModel> logger,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _hostingEnvironment = webHostEnvironment;
            _configuration = configuration;

            API_KEY = _configuration["API_KEY"];
        }

        public async void OnGet()
        {
            ErrorMessage = "geyy";
            var movie = await GetRandomMovie();
            if (movie is not null)
            {
                MovieDetail = movie;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var randomMovie = await GetRandomMovie();

            if (randomMovie is null)
            {
                var apidata = await GetTop250MoviesAsync();

                if (apidata is null)
                {
                    ErrorMessage = "No Imdb data returned";
                    return Page();
                }

                await SaveData(apidata);

                var movie = await GetRandomMovie();
                if (movie is not null)
                {
                    MovieDetail = movie;
                }

                return Page();
            }
            else
            {
                MovieDetail = randomMovie;
                return Page();
            }
        }

        private async Task<Top250Data?> GetTop250MoviesAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.GetFromJsonAsync<Top250Data>(TOP_250_URL + API_KEY);

            if (response is null)
            {
                _logger.LogError("No data returned");
                return null;
            }

            if (response.ErrorMessage != string.Empty)
            {
                _logger.LogError(response.ErrorMessage);
                return null;
            }

            return response;
        }

        private async Task SaveData(Top250Data data)
        {
            var options = new JsonSerializerOptions(_options)
            {
                WriteIndented = true
            };
            var jsonString = JsonSerializer.Serialize(data, options);

            var filepath = Path.Combine(_hostingEnvironment.ContentRootPath, FILE_NAME);
            await System.IO.File.WriteAllTextAsync(filepath, jsonString);
        }

        private async Task<Top250DataDetail?> GetRandomMovie()
        {
            try
            {
                var filepath = Path.Combine(_hostingEnvironment.ContentRootPath, FILE_NAME);
                var jsonData = await System.IO.File.ReadAllTextAsync(filepath); //read all the content inside the file

                if (string.IsNullOrWhiteSpace(jsonData)) return null; //if no data is present then return null or error if you wish

                var top250Data = JsonSerializer.Deserialize<Top250Data>(jsonData); //deserialize object as a list of users in accordance with your json file

                if (top250Data == null) return null; //if there's no data inside our list then return null or error if you wish

                var movie = top250Data.Items[Rand.Next(top250Data.Items.Count)];

                return movie;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                return null;
            }
        }
    }
}