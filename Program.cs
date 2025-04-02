using Newtonsoft.Json;
using Spectre.Console;
using System.Net.Http;
using System.Text;
using TL;
using WTelegram;
using static System.Net.WebRequestMethods;

public static class Program
{
    static Dictionary<string, string> config;

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string setting = "setting.txt";

        if (!System.IO.File.Exists(setting))
        {
            initFile(setting, "phone_number=+84\ntime=1-1\napi_id=1\napi_hash=1\n");
            AnsiConsole.Markup("[bold red]Update settings....[/]");
            return;
        }

        config = ReadSettingsFromFile("setting.txt");
        string time = config["time"];
        string phoneNumber = config["phone_number"];
        var filePath = "content.txt";
        if (!System.IO.File.Exists(filePath))
        {
            initFile(filePath, "content here");
        }
        var post = System.IO.File.ReadAllText(filePath);
       
        try
        {
            WTelegram.Helpers.Log = (lvl, str) => { };
            using var client = new WTelegram.Client(Config);
            await client.LoginUserIfNeeded();

            client.FloodRetryThreshold = 120;
            string fileName = "groups.txt";
            initFile(fileName);
            initFile(setting);
            initFile("log.txt");
            initFile("link_group.txt");
            InitDir("photos");
            while (true)
            {
                AnsiConsole.Write(new FigletText("Telegram Auto Send Group").Centered().Color(Color.Green));
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Chọn menu:")
                        .PageSize(10)
                        .AddChoices(new[] {
                        "1. Scan danh sách nhóm",
                        "2. Auto đăng bài",
                        "3. Xuất link group",
                        "4. Đăng xuất",
                        "5. Thoát"
                        })
                        .HighlightStyle(new Style(foreground: System.ConsoleColor.Yellow)));
                switch (selection)
                {
                    case "1. Scan danh sách nhóm":
                       
                        var chats = await client.Messages_GetAllChats();
                        var count = 0;
                        foreach (var (id, chat) in chats.chats)
                        {

                            if (chat is Channel channel && channel.IsGroup)
                            {
                                AppendToFile("groups.txt", $"{id,10}|{chat.Title}");
                                count++;
                            }
                            else if (chat is Chat)
                            {
                                AppendToFile("groups.txt", $"{id,10}|{chat.Title}");
                                count++;
                            }
                        }
                        Console.WriteLine($"Tổng {count} nhóm");
                        break;
                    case "2. Auto đăng bài":
                       
                        string[] parts = time.Split('-');
                        int[] numbers = Array.ConvertAll(parts, int.Parse);
                        Console.WriteLine($"Thời gian tự động đăng từ: {numbers[0]}-{numbers[1]} phút");

                        var groups = ReadAccountsFromFile("groups.txt");
                        Console.WriteLine($"Có tổng {groups.Length} nhóm");
                        int minTime = numbers[0];
                        int maxTime = numbers[1];
                        var random = new Random();
                        foreach (var group in groups)
                        {
                            // Tạo thời gian ngẫu nhiên trong khoảng 15-60 phút
                            int delayMinutes = random.Next(minTime, maxTime + 1);
                            Console.WriteLine($"Sẽ đăng vào nhóm '{group[1]}' sau {delayMinutes} phút");
                            await ShowCountdown(TimeSpan.FromMinutes(delayMinutes));

                            // Chờ đợi ngẫu nhiên trước khi đăng vào nhóm

                            // Thực hiện đăng bài vào nhóm
                            await PostToGroup(group[0], client, post);

                            Console.WriteLine($"Đã đăng vào nhóm '{group[1]}'");
                        }


                        break;
                    case "3. Xuất link group":
                        
                        var chs = await client.Messages_GetAllChats();
                        var c = 0;
                        foreach (var (id, chat) in chs.chats)
                        {
                            if (chat is Channel channel && channel.IsGroup && !string.IsNullOrEmpty(channel.username))
                            {
                                AppendToFile("link_group.txt", $"{id,10}|{chat.Title}|{(string.IsNullOrEmpty(channel.username) ? "" : $"https://t.me/{channel.username}")}");
                                c++;
                            }
                            else if (chat is Chat && !string.IsNullOrEmpty(chat.MainUsername))
                            {
                                AppendToFile("link_group.txt", $"{id,10}|{chat.Title}|{(string.IsNullOrEmpty(chat.MainUsername) ? "" : $"https://t.me/{chat.MainUsername}")}");
                                c++;
                            }
                        }
                        
                        Console.WriteLine($"Tổng {c} nhóm");
                        break;
                    case "4. Đăng xuất":
                        await client.Auth_LogOut();
                        AnsiConsole.Markup("[bold red]Đăng xuất thành công[/]");
                        break;
                    case "5. Thoát":
                        AnsiConsole.Markup("[bold red]Thoát chương trình...[/]");
                        return;
                }

                AnsiConsole.MarkupLine("\nNhấn phím bất kỳ để tiếp tục...");
                Console.ReadKey();
                AnsiConsole.Clear();
            }
        }
        catch (Exception ex)
        {
            AppendToFile("log.txt", ex.ToString());
            Console.WriteLine(ex.ToString());

        }






    }
    static async Task ShowCountdown(TimeSpan countdownTime)
    {
        for (int secondsLeft = (int)countdownTime.TotalSeconds; secondsLeft > 0; secondsLeft--)
        {
            TimeSpan remainingTime = TimeSpan.FromSeconds(secondsLeft);
            Console.Write($"\rThời gian còn lại trước khi đăng: {remainingTime:hh\\:mm\\:ss}   ");
            await Task.Delay(1000);
        }
        Console.WriteLine();
    }
    public static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        StringBuilder result = new StringBuilder();
        Random random = new Random();

        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }

        return result.ToString();
    }
    static async Task PostToGroup(string group, dynamic client, string content)
    {
        try
        {
            var chats = await client.Messages_GetAllChats();
            InputPeer peer = chats.chats[long.Parse(group)];
            var text = $"{content} {GenerateRandomString(8)}";

            if (System.IO.File.Exists(@"photos/1.png"))
            {
                var inputFile = await client.UploadFileAsync(@"photos/1.png");
                await client.SendMediaAsync(peer, text, inputFile);
                return;
            }
            await client.SendMessageAsync(peer, text);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Có lỗi xảy ra: {ex.ToString()}");
        }
    }
    static void initFile(string filePath, string content = "")
    {
        if (!System.IO.File.Exists(filePath))
        {
            System.IO.File.WriteAllText(filePath, content);
        }

    }
    static void InitDir(string filePath)
    {
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }

    }
    static void AppendToFile(string fileName, string content)
    {
        try
        {
            // Ghi thêm dòng mới vào file
            using (StreamWriter sw = System.IO.File.AppendText(fileName))
            {
                sw.WriteLine(content);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi ghi file: {ex.Message}");
        }
    }
    static string Config(string what)
    {
        switch (what)
        {
            case "api_id": return config["api_id"];
            case "api_hash": return config["api_hash"];
            case "phone_number": return config["phone_number"];
            case "verification_code": Console.Write("Code: "); return Console.ReadLine();
            case "session_pathname": return "filename.session";
            case "password": return "secret!";     
            default: return null;                 
        }
    }
    static Dictionary<string, string> ReadSettingsFromFile(string filePath)
    {
        
        string[] settings = System.IO.File.ReadAllLines(filePath);
        
        Dictionary<string, string> config = new Dictionary<string, string>();

        foreach (var line in settings)
        {
            string[] parts;
            if (line.Contains(":"))
            {
                parts = line.Split(':');
            }
            else
            {
                parts = line.Split('=');
            }

            if (parts.Length == 2)
            {
                config[parts[0].Trim()] = parts[1].Trim();
            }
        }

        return config;
    }
    static string[][] ReadAccountsFromFile(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            AnsiConsole.MarkupLine("[red]Tệp groups.txt không tồn tại.[/]");
            return new string[][] { };
        }

        // Đọc tất cả các dòng từ file
        var lines = System.IO.File.ReadAllLines(filePath);

        // Chuyển đổi mỗi dòng thành mảng [tên tài khoản, mật khẩu]
        var accounts = lines
            .Where(line => !string.IsNullOrWhiteSpace(line)) // Bỏ qua dòng trống
            .Select(line => line.Split('|')) // Tách theo ký tự |
            .ToArray(); // Chuyển thành mảng

        return accounts;
    }

}