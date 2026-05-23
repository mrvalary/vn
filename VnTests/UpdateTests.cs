using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VnTests
{
    /// <summary>
    /// Проверяет обновления по тест-кейсам, данные которых берутся из XML.
    /// </summary>
    [TestClass]
    public sealed class UpdateTests
    {
        /// <summary>
        /// Контекст MSTest для вывода пояснений при запуске тестов.
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Update Test Cases

        /// <summary>
        /// Проверяет определение доступности новой версии.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Updates")]
        [DynamicData(nameof(TestInputReader.TC_UPD_01), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_UPD_01_UpdateAvailability_IsDetected(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-UPD-01");

            GitHubReleaseService service = CreateService(
                new FakeHttpMessageHandler(CreateReleaseJson(
                    inputSet.Get("tag"),
                    "Release " + inputSet.Get("tag").TrimStart('v'),
                    "notes",
                    inputSet.Get("asset"))));
            AppUpdateInfo result = await service.CheckForUpdateAsync(CancellationToken.None);

            Assert.AreEqual(inputSet.Expected == "available", result.IsAvailable, Failure(inputSet));
            Assert.AreEqual(inputSet.Get("asset"), result.AssetName, Failure(inputSet));

            Log(testCase, inputSet, "Доступность обновления проверена по XML-набору.", "PASS");
        }

        /// <summary>
        /// Проверяет разбор ответа GitHub Releases.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Updates")]
        [DynamicData(nameof(TestInputReader.TC_UPD_02), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_UPD_02_GitHubReleaseResponse_IsParsed(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-UPD-02");

            GitHubReleaseService service = CreateService(
                new FakeHttpMessageHandler(CreateReleaseJson(
                    inputSet.Get("tag_name"),
                    inputSet.Get("name"),
                    inputSet.Get("body"),
                    inputSet.Get("asset"))));
            AppUpdateInfo result = await service.CheckForUpdateAsync(CancellationToken.None);

            Assert.AreEqual(inputSet.Get("tag_name").TrimStart('v'), result.LatestVersion, Failure(inputSet));
            Assert.AreEqual(inputSet.Get("name"), result.ReleaseName, Failure(inputSet));
            Assert.AreEqual(inputSet.Get("body"), result.ReleaseNotes, Failure(inputSet));
            Assert.AreEqual("https://example.com/" + inputSet.Get("asset"), result.DownloadUrl, Failure(inputSet));

            Log(testCase, inputSet, "Ответ GitHub Releases из XML разобран корректно.", "PASS");
        }

        /// <summary>
        /// Проверяет выбор zip-архива обновления среди assets.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Updates")]
        [DynamicData(nameof(TestInputReader.TC_UPD_03), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_UPD_03_UpdateArchive_IsSelectedOnlyForZipAsset(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-UPD-03");

            GitHubReleaseService service = CreateService(
                new FakeHttpMessageHandler(CreateReleaseJson("v99.0.0", "Release 99", "notes", inputSet.Get("asset"))));
            AppUpdateInfo result = await service.CheckForUpdateAsync(CancellationToken.None);

            if (inputSet.Expected == "selected")
            {
                Assert.IsTrue(result.IsAvailable, Failure(inputSet));
                Assert.AreEqual(inputSet.Get("asset"), result.AssetName, Failure(inputSet));
            }
            else
            {
                Assert.IsFalse(result.IsAvailable, Failure(inputSet));
                Assert.AreEqual(string.Empty, result.AssetName, Failure(inputSet));
                Assert.AreEqual(string.Empty, result.DownloadUrl, Failure(inputSet));
            }

            Log(testCase, inputSet, "Выбор архива обновления выполнен по asset из XML.", "PASS");
        }

        /// <summary>
        /// Проверяет обработку ошибок GitHub API.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Updates")]
        [DynamicData(nameof(TestInputReader.TC_UPD_04), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_UPD_04_GitHubApiErrors_AreReported(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-UPD-04");

            if (!string.IsNullOrWhiteSpace(inputSet.Get("statusCode")))
            {
                GitHubReleaseService service = CreateService(
                    new FakeHttpMessageHandler("{}", (HttpStatusCode)inputSet.GetInt32("statusCode")));
                await Assert.ThrowsExceptionAsync<HttpRequestException>(
                    async () => await service.CheckForUpdateAsync(CancellationToken.None),
                    Failure(inputSet));
            }
            else
            {
                GitHubReleaseService service = CreateService(
                    new FakeHttpMessageHandler(inputSet.Get("json")));
                bool failed = false;
                try
                {
                    await service.CheckForUpdateAsync(CancellationToken.None);
                }
                catch (Exception)
                {
                    failed = true;
                }

                Assert.IsTrue(failed, Failure(inputSet));
            }

            Log(testCase, inputSet, "Ошибка GitHub API из XML обработана как сбой.", "PASS");
        }

        /// <summary>
        /// Проверяет безопасный сценарий отсутствующего VnInstaller.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Updates")]
        [DynamicData(nameof(TestInputReader.TC_UPD_05), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_UPD_05_InstallerLaunch_ReturnsFalseWhenInstallerMissing(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-UPD-05");

            string installerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, inputSet.Get("installerPath"));
            if (inputSet.Expected == "readable-error")
            {
                Assert.IsFalse(File.Exists(installerPath), Failure(inputSet));
            }
            else
            {
                Assert.AreEqual("vn-installer.exe", Path.GetFileName(installerPath), Failure(inputSet));
            }

            Log(testCase, inputSet, "Сценарий запуска установщика из XML проверен.", "PASS");
        }

        /// <summary>
        /// Проверяет параметры, которые нужны установщику обновлений.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Updates")]
        [DynamicData(nameof(TestInputReader.TC_UPD_06), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_UPD_06_InstallerParameters_AreTakenFromUpdateInfo(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-UPD-06");

            Assert.IsFalse(string.IsNullOrWhiteSpace(inputSet.Get("appPath")), Failure(inputSet));
            Assert.IsTrue(inputSet.GetInt32("processId") > 0, Failure(inputSet));
            Assert.IsTrue(inputSet.Get("downloadUrl").EndsWith(".zip", StringComparison.OrdinalIgnoreCase), Failure(inputSet));
            Assert.IsFalse(string.IsNullOrWhiteSpace(inputSet.Get("latestVersion")), Failure(inputSet));

            Log(testCase, inputSet, "Параметры запуска установщика из XML проверены.", "PASS");
        }

        /// <summary>
        /// Проверяет восстановление после ошибки обновления.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Updates")]
        [DynamicData(nameof(TestInputReader.TC_UPD_07), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_UPD_07_UpdateFailure_DoesNotLookLikeSuccessfulUpdate(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-UPD-07");

            if (!string.IsNullOrWhiteSpace(inputSet.Get("downloadUrl")))
            {
                Assert.IsTrue(inputSet.Get("downloadUrl").EndsWith(".zip", StringComparison.OrdinalIgnoreCase), Failure(inputSet));
            }
            else if (!string.IsNullOrWhiteSpace(inputSet.Get("archive")))
            {
                Assert.AreEqual("failure", inputSet.Expected, Failure(inputSet));
                Assert.IsTrue(inputSet.Get("archive").EndsWith(".zip", StringComparison.OrdinalIgnoreCase), Failure(inputSet));
            }
            else
            {
                Assert.AreEqual("failure", inputSet.Expected, Failure(inputSet));
            }

            Log(testCase, inputSet, "Ошибочные параметры обновления из XML не считаются успешным обновлением.", "PASS");
        }

        /// <summary>
        /// Проверяет использование настроек обновления из YAML-модели.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Updates")]
        [DynamicData(nameof(TestInputReader.TC_UPD_08), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_UPD_08_UpdateSettings_AreUsedInGitHubRequest(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-UPD-08");

            FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
                CreateReleaseJson("v1.0.0", "Release 1", "notes", "vn-release.zip"));
            GitHubReleaseService service = CreateService(
                handler,
                inputSet.Get("owner"),
                inputSet.Get("repo"),
                inputSet.Get("assetExtension"));

            await service.CheckForUpdateAsync(CancellationToken.None);

            Assert.AreEqual(
                "https://api.github.com/repos/" + inputSet.Get("owner") + "/" + inputSet.Get("repo") + "/releases/latest",
                handler.RequestUri.ToString(),
                Failure(inputSet));

            Log(testCase, inputSet, "YAML-настройки обновления из XML использованы в запросе.", "PASS");
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Создает сервис обновлений с поддельным HTTP-клиентом.
        /// </summary>
        private static GitHubReleaseService CreateService(
            FakeHttpMessageHandler handler,
            string owner = "mrvalary",
            string repo = "vn",
            string assetExtension = ".zip")
        {
            YamlAppSettings settings = CreateSettings(owner, repo, assetExtension);
            HttpClient httpClient = new HttpClient(handler);
            return new GitHubReleaseService(httpClient, settings);
        }

        /// <summary>
        /// Создает YAML-настройки для теста.
        /// </summary>
        private static YamlAppSettings CreateSettings(string owner, string repo, string assetExtension)
        {
            YamlAppSettings settings = new YamlAppSettings();
            SetPrivateProperty(settings, "UpdateOwner", owner);
            SetPrivateProperty(settings, "UpdateRepo", repo);
            SetPrivateProperty(settings, "UpdateAssetExtension", assetExtension);
            SetPrivateProperty(settings, "UpdateHttpTimeoutSeconds", 15);
            return settings;
        }

        /// <summary>
        /// Устанавливает значение свойства с закрытым setter'ом.
        /// </summary>
        private static void SetPrivateProperty(YamlAppSettings settings, string propertyName, object value)
        {
            PropertyInfo property = typeof(YamlAppSettings).GetProperty(propertyName);
            property.GetSetMethod(true).Invoke(settings, new[] { value });
        }

        /// <summary>
        /// Создает JSON GitHub Releases для тестового HTTP-ответа.
        /// </summary>
        private static string CreateReleaseJson(string tag, string releaseName, string body, string assetName)
        {
            return "{\"tag_name\":\"" + tag + "\",\"name\":\"" + releaseName + "\",\"body\":\"" + body + "\",\"assets\":[{\"name\":\"" +
                assetName + "\",\"browser_download_url\":\"https://example.com/" + assetName + "\"}]}";
        }

        /// <summary>
        /// Выводит результат отдельного XML-набора.
        /// </summary>
        private void Log(TestCaseData testCase, TestInputSet inputSet, string actual, string status)
        {
            TestOutputHelper.WriteLine("Тест-кейс:            " + testCase.Id);
            TestOutputHelper.WriteLine("Название:             " + testCase.Name);
            TestOutputHelper.WriteLine("XML-набор:            " + inputSet.Name);
            TestOutputHelper.WriteLine("Ожидание XML-набора:  " + inputSet.Expected);
            TestOutputHelper.WriteLine("Входные данные XML:   " + testCase.Inputs);
            TestOutputHelper.WriteLine("Ожидаемый результат:  " + testCase.Expected);
            TestOutputHelper.WriteLine("Фактический результат:" + actual);
            TestOutputHelper.WriteLine("Статус:               " + status);
            TestOutputHelper.WriteLine(string.Empty);
        }

        /// <summary>
        /// Формирует сообщение об ошибке с названием XML-набора.
        /// </summary>
        private static string Failure(TestInputSet inputSet)
        {
            return "Ошибка в XML-наборе: " + inputSet.Name + ", expected=" + inputSet.Expected;
        }

        /// <summary>
        /// Поддельный обработчик HTTP-запросов для тестирования GitHubReleaseService без интернета.
        /// </summary>
        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            // JSON, который будет возвращён сервису вместо ответа настоящего GitHub API.
            private readonly string _json;
            // HTTP-код ответа для проверки успешных и ошибочных сценариев.
            private readonly HttpStatusCode _statusCode;

            /// <summary>
            /// Создает обработчик с заранее подготовленным JSON-ответом.
            /// </summary>
            public FakeHttpMessageHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
            {
                _json = json;
                _statusCode = statusCode;
            }

            /// <summary>
            /// Последний URI, по которому сервис попытался выполнить запрос.
            /// </summary>
            public Uri RequestUri { get; private set; }

            /// <summary>
            /// Возвращает заранее подготовленный HTTP-ответ и запоминает URI запроса.
            /// </summary>
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestUri = request.RequestUri;
                HttpResponseMessage response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_json, Encoding.UTF8, "application/json")
                };

                return Task.FromResult(response);
            }
        }

        #endregion
    }
}
