using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Drawing;

/// <summary>Light fills used to distinguish room types on plans and in the editor.</summary>
public static class RoomColors
{
    public static string For(RoomType t) => t switch
    {
        RoomType.Living or RoomType.Lounge => "#fdebd0",
        RoomType.Kitchen or RoomType.Utility => "#fadbd8",
        RoomType.Dining => "#fcf3cf",
        RoomType.MasterBedroom or RoomType.Bedroom => "#d6eaf8",
        RoomType.Bath => "#d1f2eb",
        RoomType.Pooja => "#fff2b3",
        RoomType.Study => "#e8daef",
        RoomType.Store => "#e5e7e9",
        RoomType.Staircase => "#d5d8dc",
        RoomType.Parking => "#eaeded",
        RoomType.Balcony => "#d5f5e3",
        _ => "#f8f9f9",
    };
}
