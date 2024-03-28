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
				Directory.CreateDirectory(path); //B�yle bir dosya yoksa olu�tur.
			}

			FileStream fileStream = null;

			try
			{
				//gelecek olan stream datay� yakalama i�lemi
				while(await requestStream.MoveNext())
				{
					int count = 0;

					decimal chunkSize = 0;

					if(count++ == 0)
					{
						fileStream = new FileStream($"{path}/{requestStream.Current.Info.FileName}{requestStream.Current.Info.FileExtension}",FileMode.CreateNew); ;
						fileStream.SetLength(requestStream.Current.FileSize);
						var buffer = requestStream.Current.Buffer.ToByteArray(); //gelecek olan b�t�n�n bir par�as�
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
			//par�ay� oku g�nder
			while((content.ReadedByte = await fileStream.ReadAsync(buffer,0,buffer.Length))>0)  // 0'dan k���k de�er kalmad� (okunacak de�er yok.)
			{
				content.Buffer = ByteString.CopyFrom(buffer);
				await responseStream.WriteAsync(content);
			}
			fileStream.Close();
		}
	}
}
