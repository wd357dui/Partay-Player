using Microsoft.AspNetCore.StaticFiles;
using System.Net;
using System.Text.Json;

namespace PartayPlayer
{
	public partial class PartayPlayer : Form
	{
		public PartayPlayer()
		{
			Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--autoplay-policy=no-user-gesture-required");
			InitializeComponent();
		}

		public string Directory = ".\\";
		public List<string> Files = [];
		int FileIndex = 0;

		private Rectangle PreviousWindowedBounds;

		private string enableTimeout = "true";
		private string msTimeout = "7000";
		private string controls = "";
		private string loop = "";

		private readonly HttpListener server = new();
		public const string Localhost = "http://localhost:5393/";

		private void FillFiles()
		{
			Files.Clear();
			foreach (FileInfo f in
			from FileInfo file in new DirectoryInfo(Directory).EnumerateFiles()
			where !file.Name.Equals("index.html", StringComparison.InvariantCultureIgnoreCase)
			orderby file.CreationTime ascending
			select file
			)
			{
				Files.Add(f.FullName);
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			var Init = InitializeAsync();
			using FolderBrowserDialog Folder = new();
			if (Folder.ShowDialog() == DialogResult.OK)
			{
				Directory = Folder.SelectedPath;
			}
			FillFiles();
			server.Prefixes.Add(Localhost);
			server.Start();
			server.BeginGetContext(Respond, server);
			web.WebMessageReceived += Web_WebMessageReceived;
			Init.Wait();
			Nav();
			Activate();
		}

		private void Respond(IAsyncResult result)
		{
			HttpListenerContext context = server.EndGetContext(result);
			new Thread(() =>
			{
				var path = Uri.UnescapeDataString(context.Request.Url!.AbsolutePath);
				path = Path.Combine(Directory, path.Replace("/", "").Replace("\\", ""));
				new FileExtensionContentTypeProvider().TryGetContentType(context.Request.Url!.AbsolutePath, out string? ContentType);
				context.Response.ContentType = ContentType;
				if (context.Request.KeepAlive)
				{
					context.Response.KeepAlive = true;
				}
				try
				{
					using FileStream f = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					var filesize = f.Length;
					if (context.Request.Headers.Get("Range") is string Range)
					{
						// Parse the Range header to determine the requested byte range
						string[] rangeParts = Range.Replace("bytes=", "").Split('-');
						if (long.TryParse(rangeParts[0], out long start))
						{
							long end = filesize - 1;
							if (rangeParts.Length == 2 && long.TryParse(rangeParts[1], out long endAt))
							{
								end = Math.Min(endAt, filesize - 1);
							}
							// Respond range request
							context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
							context.Response.AddHeader("Content-Range", $"bytes {start}-{end}/{filesize}");
							context.Response.ContentLength64 = end - start + 1;
							f.CopyStreamPortion(context.Response.OutputStream, start, end);
						}
						else
						{
							// Invalid range request
							context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
						}
					}
					else
					{
						context.Response.ContentLength64 = filesize;
						f.CopyTo(context.Response.OutputStream);
					}
				}
				catch (HttpListenerException)
				{ }
				catch (FileNotFoundException)
				{
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;
				}
				catch (IOException)
				{
					context.Response.StatusCode = (int)HttpStatusCode.Conflict;
				}
				finally
				{
					context.Response.OutputStream.Flush();
					context.Response.Close();
				}
			}).Start();
			server.BeginGetContext(Respond, server);
		}

		private async Task InitializeAsync()
		{
			await web.EnsureCoreWebView2Async(null);
		}

		private void Nav()
		{
			string html = Generate(FileIndex);
			string IndexPath = Path.Combine(Directory, "index.html");
			using (StreamWriter sw = new(IndexPath, false, System.Text.Encoding.UTF8))
			{
				sw.Write(html);
			}
			if (!string.IsNullOrEmpty(IsFlash(FileIndex)))
			{
				web.CoreWebView2.Navigate($"{Localhost}index.html");
			}
			else
			{
				web.CoreWebView2.Navigate($"file:///{IndexPath}");
			}
		}

		private string Generate(int n) => @$"
<html>
	<meta charset=""UTF-8"">
	<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
	<style>
	body {{
		margin: 0;
		padding: 0;
		height: 100vh; /* Set the body height to 100% of the viewport height */
		display: flex;
		justify-content: center;
		align-items: center;
	}}

	.content {{
		width: 100%;
		height: 100%;
		object-fit: contain;
	}}
	</style>
	{IsFlash(n)}
	<script>
		function NextPage() {{
			const endInfo = {{
				ended: true
			}};
			window.chrome.webview.postMessage(endInfo);
		}}
		document.addEventListener(""DOMContentLoaded"", (event) => {{
			document.addEventListener('keyup', function (event) {{
				const pressedKey = event.key;
				const keyInfo = {{
					pressedKey: pressedKey
				}};
				window.chrome.webview.postMessage(keyInfo);
			}});
			var elements = document.getElementsByName(""src"");
			elements.forEach(function(element) {{
				let currentSrc = element.getAttribute(""src"");
				let unescapedSrc = decodeURIComponent(currentSrc).replaceAll('\\','/');
				element.src = unescapedSrc;
				if (element.src != unescapedSrc && element.src != (""file:///"" + unescapedSrc)) {{
					console.log(""url unescape failed!!!"");
					console.log(""element.src="" + element.src);
					console.log(""unescapedSrc="" + unescapedSrc);
					console.log(""fallback to http web requrest"");

					let fileName = unescapedSrc.split(/[\\/]/).pop();
					console.log(""fileName="" + fileName);
					element.src = ""{Localhost}"" + encodeURIComponent(fileName);
					console.log(element.src);
				}}
			}});
			var elements = document.getElementsByName(""player"");
			elements.forEach(function(element) {{
				element.onended = NextPage;
			}});
			if (elements.length === 0 && {enableTimeout}) {{
				setTimeout(NextPage, {msTimeout});
			}}
		}});
	</script>
	<body style=""margin: 0; padding: 0;"">
		{Media(n)}
	</body>
</html>
";
		private string Media(int n)
		{
			string File = Files[n];
			if (File.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".m4v", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".webm", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".wmv", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".mov", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".flv", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".mkv", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".mpg", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".ts", StringComparison.InvariantCultureIgnoreCase))
			{
				return Video(n);
			}
			else if (File.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".ogg", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".weba", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".flac", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".m4a", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".mka", StringComparison.InvariantCultureIgnoreCase))
			{
				return Audio(n);
			}
			else if (File.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".jfif", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase) ||
				File.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase))
			{
				return Image(n);
			}
			else if (File.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase))
			{
				return Txt(n);
			}
			else if (File.EndsWith(".swf", StringComparison.InvariantCultureIgnoreCase))
			{
				return Flash(n);
			}
			else return Other(n);
		}
		private string Video(int n) => $@"
<video name=""player"" class=""content"" {controls} {loop} autoplay>
	<source name=""src"" src=""{Files[n]}"" type=""video/{Path.GetExtension(Files[n]).TrimStart('.').ToLower()}"">
</video>
";
		private string Audio(int n) => $@"
<audio name=""player"" class=""content"" {controls} {loop} autoplay>
	<source name=""src"" src=""{Files[n]}"" type=""audio/{Path.GetExtension(Files[n]).TrimStart('.').ToLower()}"">
</audio>
";
		private string Image(int n) => $@"
<img class=""content"" name=""src"" src=""{Files[n]}"" alt=""{Files[n]}"">
";
		private string Txt(int n) => $@"
<pre>
{File.ReadAllText(Files[n])}
</pre>
";
		private string IsFlash(int n) =>
			Path.GetFileName(Files[n]).EndsWith(".swf", StringComparison.InvariantCultureIgnoreCase) ?
$@"
<script src=""https://unpkg.com/@ruffle-rs/ruffle""></script>
" : "";
		private string Flash(int n) => $@"
<embed name=""player"" class=""content"" name=""src"" src=""{Localhost}{Path.GetFileName(Files[n])}"">
";
		private string Other(int n) => $@"
<h1><a href=""{Files[n]}"">{Files[n]}</a></h1>
";

		private void Web_WebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
		{
			string json = e.WebMessageAsJson;
			var result = JsonDocument.Parse(json);
			result.RootElement.TryGetProperty("pressedKey", out JsonElement pressedKey);
			result.RootElement.TryGetProperty("ended", out JsonElement ended);
			string key = pressedKey.ValueKind == JsonValueKind.String ? pressedKey.GetString() ?? "" : "";
			bool end = ended.ValueKind != JsonValueKind.Null && ended.ValueKind != JsonValueKind.False && ended.ValueKind != JsonValueKind.Undefined;

			if (key == " ") key = "Space";
			if (key.Contains("Arrow")) key = key.Replace("Arrow", "");

			if (Keys.F5.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase))
			{
				string PreviousIndex = Files[FileIndex];
				FillFiles();
				FileIndex = int.Max(0, Files.IndexOf(PreviousIndex));
			}
			else if (Keys.F11.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase))
			{
				if (FormBorderStyle == FormBorderStyle.None)
				{
					FormBorderStyle = FormBorderStyle.Sizable;
					Bounds = PreviousWindowedBounds;
				}
				else
				{
					PreviousWindowedBounds = Bounds;
					FormBorderStyle = FormBorderStyle.None;
					DesktopBounds = Screen.FromControl(this).Bounds;
				}
			}
			else if (end ||
				Keys.Space.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase) ||
				Keys.PageDown.ToString().Replace("Next", "PageDown").Equals(key, StringComparison.InvariantCultureIgnoreCase) ||
				Keys.MediaNextTrack.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase) ||
				Keys.Right.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase) ||
				Keys.Down.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase))
			{
				FileIndex = int.Min(FileIndex + 1, Files.Count - 1);
				Nav();
			}
			else if (
				Keys.PageUp.ToString().Replace("Prior", "PageUp").Equals(key, StringComparison.InvariantCultureIgnoreCase) ||
				Keys.MediaPreviousTrack.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase) ||
				Keys.Left.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase) ||
				Keys.Up.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase))
			{
				FileIndex = int.Max(FileIndex - 1, 0);
				Nav();
			}
			else if (Keys.F.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase))
			{
				OpenFileDialog FileDialog = new()
				{
					Multiselect = false,
					InitialDirectory = Directory,
					Filter = "Any|*.*",
					FileName = Path.GetFileName(Files[FileIndex])
				};
				if (FileDialog.ShowDialog() == DialogResult.OK)
				{
					int index = Files.IndexOf(FileDialog.FileName);
					if (index != -1)
					{
						FileIndex = index;
						Nav();
					}
				}
			}
			else if (Keys.C.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase))
			{
				controls = controls == "" ? "controls" : "";
				Nav();
			}
			else if (Keys.L.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase))
			{
				loop = loop == "" ? "loop" : "";
				Nav();
			}
			else if (Keys.S.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase))
			{
				enableTimeout = enableTimeout == "false" ? "true" : "false";
				Nav();
			}
		}
	}

	public static class Extend
	{
		public static void CopyStreamPortion(this Stream Src, Stream Dst, long Begin, long End)
		{
			Src.Position = Begin;
			byte[] buffer = new byte[4096];
			while (Src.Position <= End)
			{
				int Read = Src.Read(buffer, 0, int.Min(4096, (int)(End - Src.Position + 1)));
				Dst.Write(buffer, 0, Read);
			}
		}
	}
}
