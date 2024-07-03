using System.Reflection;

namespace SadieTester.Networking.Packets.Events;

public class EventSerializer
{
    public static void SetPropertiesForEventHandler(object handler, INetworkPacket packet)
    {
        var t = handler.GetType();
        var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var type = property.PropertyType;

            if (type == typeof(int) || type == typeof(long))
            {
                property.SetValue(handler, packet.ReadInteger(), null);
            }
            else if (type == typeof(string))
            {
                property.SetValue(handler, packet.ReadString(), null);
            }
            else if (type == typeof(bool))
            {
                property.SetValue(handler, packet.ReadBool(), null);
            }
            else if (type == typeof(List<string>))
            {
                property.SetValue(handler, ReadStringList(packet), null);
            }
            else if (type == typeof(List<int>))
            {
                property.SetValue(handler, ReadIntegerList(packet), null);
            }
            else if (type == typeof(Dictionary<string, string>))
            {
                property.SetValue(handler, ReadAllStringDictionary(packet), null);
            }
            else
            {
                throw new Exception($"{type.FullName}");
            }
        }
    }

    private static Dictionary<string, string> ReadAllStringDictionary(INetworkPacket packet)
    {
        var temp = new Dictionary<string, string>();
        var amount = packet.ReadInteger();

        for (var i = 0; i < amount / 2; i++)
        {
            temp[packet.ReadString()] = packet.ReadString();
        }

        return temp;
    }

    private static List<int> ReadIntegerList(INetworkPacketReader packet)
    {
        var tempList = new List<int>();
        var amount = packet.ReadInteger();

        for (var i = 0; i < amount; i++)
        {
            tempList.Add(packet.ReadInteger());
        }

        return tempList;
    }

    private static List<string> ReadStringList(INetworkPacketReader packet)
    {
        var tempList = new List<string>();

        for (var i = 0; i < packet.ReadInteger(); i++)
        {
            tempList.Add(packet.ReadString());
        }

        return tempList;
    }
}