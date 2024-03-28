using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using grpcFileTransportServer;
using grpcServer;

namespace grpcServer.Services
{
	public class FileTransportService : FileService.FileServiceBase
	{
        readonly IWebHostEnvironment _webHostEnvironment;

		public FileTransportService(IWebHostEnvironment webHostEnvironment)
		{
			_webHostEnvironment = webHostEnvironment;
		}

		public override async Task<Empty> FileUpload(IAsyncStreamReader<BytesContent> requestStream, ServerCallContext context)
		{
			//Stream
			string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");

			if(!Directory.Exists(path))
			{
				Directory.CreateDirectory(path); //Böyle bir dosya yoksa oluþtur.
			}

			FileStream fileStream = null;

			try
			{
				//gelecek olan stream datayý yakalama iþlemi
				while(await requestStream.MoveNext())
				{
					int count = 0;

					decimal chunkSize = 0;

					if(count++ == 0)
					{
						fileStream = new FileStream($"{path}/{requestStream.Current.Info.FileName}{requestStream.Current.Info.FileExtension}",FileMode.CreateNew); ;
						fileStream.SetLength(requestStream.Current.FileSize);
						var buffer = requestStream.Current.Buffer.ToByteArray(); //gelecek olan bütünün bir parçasý
						await fileStream.WriteAsync(buffer, 0, buffer.Length);

						System.Console.WriteLine($"{Math.Round(((chunkSize += requestStream.Current.ReadedByte) * 100) / requestStream.Current.FileSize)}%");
					}
				}

			}
			catch
			{

			}

			await fileStream.DisposeAsync();
			fileStream.Close();
			return new Empty();
		}

		public override async Task FileDownload(grpcFileTransportServer.FileInfo request, IServerStreamWriter<BytesContent> responseStream, ServerCallContext context)
		{
			string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");

			FileStream fileStream = new FileStream($"{path}/{request.FileName}{request.FileExtension}",FileMode.Open,FileAccess.Read);

			byte[] buffer = new byte[2048]; //max

			BytesContent content = new BytesContent
			{
				FileSize = fileStream.Length,
				Info = new grpcFileTransportServer.FileInfo {FileName = Path.GetFileNameWithoutExtension(fileStream.Name) , FileExtension = Path.GetExtension(fileStream.Name)},
				ReadedByte = 0,
			};
			//parçayý oku gönder
			while((content.ReadedByte = await fileStream.ReadAsync(buffer,0,buffer.Length))>0)  // 0'dan küçük deðer kalmadý (okunacak deðer yok.)
			{
				content.Buffer = ByteString.CopyFrom(buffer);
				await responseStream.WriteAsync(content);
			}
			fileStream.Close();
		}
	}
}
