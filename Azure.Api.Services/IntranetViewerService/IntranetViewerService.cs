using Azure.Api.Data.DTOs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public class IntranetViewerService : IIntranetViewerService
    {
        private readonly IConfiguration _config;

        public IntranetViewerService(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<Tuple<int, IHttpMessage>> TestCommunicationWithLpcrApi()
        {
            string apiUrl;
            try
            {
                var apiSection = _config.GetSection("APIs");
                var urlSection = apiSection.GetSection("LPCR_Entrypoints").GetSection("Test");

                apiUrl = $"{ apiSection.GetValue<string>("LPCR") }/{ urlSection.GetValue<string>("url") }";
            }

            catch (Exception)
            {
                return Tuple.Create(500, new HttpErrorMessage("Appsettings doesn't  contains the neccessary infos to construct the entrypoint url") as IHttpMessage);
            }

            return await CallGetAPI(apiUrl, string.Empty);
        }

        public async Task<Tuple<int, IHttpMessage>> ListLpcrClients(string search, int page, int pageSize)
        {
            string apiUrl;
            string rqstUri;
            try
            {
                var apiSection = _config.GetSection("APIs");
                var urlSection = apiSection.GetSection("LPCR_Entrypoints").GetSection("ListOfClients");

                apiUrl =  $"{ apiSection.GetValue<string>("LPCR") }/{ urlSection.GetValue<string>("url") }";
                rqstUri = String.Format(urlSection.GetValue<string>("rqstUri"), search, page, pageSize);
            }

            catch (Exception)
            {
                return Tuple.Create(500, new HttpErrorMessage("Appsettings doesn't  contains the neccessary infos to construct the entrypoint url") as IHttpMessage);
            }

            return await CallGetAPI(apiUrl, rqstUri);
        }

        public async Task<Tuple<int, IHttpMessage>> ListLpcrRepriseDocuments(int page, int pageSize)
        {
            string apiUrl;
            string rqstUri;
            try
            {
                var apiSection = _config.GetSection("APIs");
                var urlSection = apiSection.GetSection("LPCR_Entrypoints").GetSection("ListRepriseDocuments");

                apiUrl = $"{ apiSection.GetValue<string>("LPCR") }/{ urlSection.GetValue<string>("url") }";
                rqstUri = String.Format(urlSection.GetValue<string>("rqstUri"), page, pageSize);
            }

            catch (Exception)
            {
                return Tuple.Create(500, new HttpErrorMessage("Appsettings doesn't  contains the neccessary infos to construct the entrypoint url") as IHttpMessage);
            }

            return await CallGetAPI(apiUrl, rqstUri);
        }

        public async Task<Tuple<int, IHttpMessage>> DownloadLpcrRepriseDocument(string path)
        {
            string apiUrl;

            try
            {
                var apiSection = _config.GetSection("APIs");
                var urlSection = apiSection.GetSection("LPCR_Entrypoints").GetSection("DownloadRepriseDocuments");

                apiUrl = $"{ apiSection.GetValue<string>("LPCR") }/{ urlSection.GetValue<string>("url") }";
            }

            catch (Exception)
            {
                 return Tuple.Create(500, new HttpErrorMessage("Appsettings doesn't  contains the neccessary infos to construct the entrypoint url") as IHttpMessage);
            }

            var body = new
            {
                Path = path
            };

            return await CallPostAPI<object>(apiUrl, "", body);
        }

        private async Task<Tuple<int, IHttpMessage>> CallGetAPI(string apiUrl, string rqstUri)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var resp = await client.GetAsync(rqstUri))
                {
                    if (resp.IsSuccessStatusCode)
                    {
                        return Tuple.Create((int)resp.StatusCode, new HttpSuccessMessage(await resp.Content.ReadAsStringAsync()) as IHttpMessage);
                    }

                    else
                    {
                        return Tuple.Create((int)resp.StatusCode, new HttpErrorMessage(resp.ReasonPhrase) as IHttpMessage);
                    }
                }
            }
        }

        private async Task<Tuple<int, IHttpMessage>> CallPostAPI<T>(string apiUrl, string rqstUri, T body)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var serializedBody = JsonConvert.SerializeObject(body);
                var buffer = System.Text.Encoding.UTF8.GetBytes(serializedBody);

                using (var payload = new ByteArrayContent(buffer)) 
                {
                    payload.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    using (var resp = await client.PostAsync(rqstUri, payload))
                    {
                        if (resp.IsSuccessStatusCode)
                        {
                            return Tuple.Create((int)resp.StatusCode, new HttpSuccessMessage(await resp.Content.ReadAsStringAsync()) as IHttpMessage);
                        }

                        else
                        {
                            return Tuple.Create((int)resp.StatusCode, new HttpErrorMessage(resp.ReasonPhrase) as IHttpMessage);
                        }
                    }
                }    
            }
        }
    }
}
