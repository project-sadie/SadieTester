using SadieTester.Networking.Packets.Events;

namespace SadieTester.Networking.Packets.Writers.Navigator;

public class NavigatorSearchWriter : NetworkPacketWriter
{
    public NavigatorSearchWriter()
    {
        WriteShort(ServerPacketIds.NavigatorSearch);
        WriteString("hotel_view");
        WriteString("");
    }
}