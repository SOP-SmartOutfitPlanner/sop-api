using System;

namespace SOPServer.Service.BusinessModels.UserDeviceModels
{
    public class UserDeviceModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string DeviceToken { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserDeviceModel
    {
        public long UserId { get; set; }
        public string DeviceToken { get; set; }
    }
}
