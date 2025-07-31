using SadieTester.Networking.Packets.Events;

namespace SadieTester.Networking.Packets.Writers.Rooms;

public class CreateRoomWriter : NetworkPacketWriter
{
    public CreateRoomWriter(CreateRoomData data, int maxUsers = 50)
    {
        WriteShort(ServerPacketIds.PlayerCreateRoom);
        WriteString(data.Name);
        WriteString(data.Description);
        WriteString(data.Layout);
        WriteInteger(0);
        WriteInteger(maxUsers);
        WriteInteger(0);
    }
}

public class CreateRoomData
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Layout { get; set; }
}

public class ModelSelector
{
    private static readonly List<string> models = new List<string>
    {
        "model_basa", "model_4", "model_3", "model_b2g", "model_opening",
        "model_y", "model_oscar", "model_u", "model_z", "model_w",
        "model_x", "model_0", "model_v", "model_t", "model_s",
        "model_h", "model_p", "model_r", "model_q", "model_o",
        "model_n", "model_g", "model_l", "model_m", "model_k",
        "model_j", "model_i", "model_e", "model_f", "model_a",
        "model_b", "model_c", "model_d", "model_room_15", "model_1",
        "model_2", "model_5", "model_6", "model_7"
    };

    private static readonly Random random = new Random();

    public static string GetRandomModel()
    {
        int index = random.Next(models.Count);
        return models[index];
    }
}