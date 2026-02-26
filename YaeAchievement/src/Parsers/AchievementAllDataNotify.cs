using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Spectre.Console;
using YaeAchievement.Utilities;

namespace YaeAchievement.Parsers;

public enum AchievementStatus {
    Invalid,
    Unfinished,
    Finished,
    RewardTaken,
}

public sealed class AchievementItem {
    
    public uint Id { get; init; }
    public uint TotalProgress { get; init; }
    public uint CurrentProgress { get; init; }
    public uint FinishTimestamp { get; init; }
    public AchievementStatus Status { get; init; }
    
}

public sealed class AchievementAllDataNotify {

    public List<AchievementItem> AchievementList { get; private init; } = [];
    
    private static AchievementAllDataNotify? Instance { get; set; }

    public static bool OnReceive(BinaryReader reader) {
        var bytes = reader.ReadBytes();
        CacheFile.Write("achievement_data", bytes);
        Instance = ParseFrom(bytes);
        return true;
    }

    public static void OnFinish() {
        if (Instance == null) {
            throw new ApplicationException("No data received");
        }
        Export.Choose(Instance);
    }

    public static AchievementAllDataNotify ParseFrom(byte[] bytes) {
        using var stream = new CodedInputStream(bytes);
        var data = new List<Dictionary<uint, uint>>();
        var errTimes = 0;
        try {
            uint tag;
            while ((tag = stream.ReadTag()) != 0) {
                if ((tag & 7) == 2) { // is LengthDelimited
                    var dict = new Dictionary<uint, uint>();
                    using var eStream = stream.ReadLengthDelimitedAsStream();
                    try {
                        while ((tag = eStream.ReadTag()) != 0) {
                            if ((tag & 7) != 0) { // not VarInt
                                dict = null;
                                break;
                            }
                            dict[tag >> 3] = eStream.ReadUInt32();
                        }
                        if (dict is { Count: > 2 }) { // at least 3 fields
                            data.Add(dict);
                        }
                    } catch (InvalidProtocolBufferException) {
                        if (errTimes++ > 0) { // allows 1 fail on 'reward_taken_goal_id_list'
                            throw;
                        }
                    }
                }
            }
        } catch (InvalidProtocolBufferException) {
            // ReSharper disable once LocalizableElement
            AnsiConsole.WriteLine("Parse failed");
            File.WriteAllBytes("achievement_raw_data.bin", bytes);
            Environment.Exit(0);
        }
        if (data.Count == 0) {
            return new AchievementAllDataNotify();
        }
        var pb = GlobalVars.AchievementInfo.PbInfo;
        return new AchievementAllDataNotify {
            AchievementList = data.Select(dict => new AchievementItem {
                Id = dict[pb.Id],
                Status = (AchievementStatus) dict[pb.Status],
                TotalProgress = dict[pb.TotalProgress],
                CurrentProgress = dict.GetValueOrDefault(pb.CurrentProgress),
                FinishTimestamp = dict.GetValueOrDefault(pb.FinishTimestamp),
            }).ToList()
        };
    }

}

[JsonSerializable(typeof(AchievementAllDataNotify))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Serialization,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
)]
public sealed partial class AchievementRawDataSerializer : JsonSerializerContext {

    public static string Serialize(AchievementAllDataNotify ntf) {
        return JsonSerializer.Serialize(ntf, Default.AchievementAllDataNotify);
    }
}
