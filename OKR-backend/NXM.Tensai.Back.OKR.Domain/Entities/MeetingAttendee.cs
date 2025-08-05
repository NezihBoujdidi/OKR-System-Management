using System;

namespace NXM.Tensai.Back.OKR.Domain.Entities
{
    public class MeetingAttendee
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public int UserId { get; set; }
        
        // Status tracking
        public bool HasJoined { get; set; } = false;
        public DateTime? JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        
        // Response status
        public string ResponseStatus { get; set; } = "pending"; // pending, accepted, declined
        public DateTime? RespondedAt { get; set; }
        
        // Navigation properties
        public virtual Meeting Meeting { get; set; }
        // Assuming you have a User entity
        // public virtual User User { get; set; }
    }
} 