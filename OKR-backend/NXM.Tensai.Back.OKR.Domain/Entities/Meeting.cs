using System;
using System.Collections.Generic;

namespace NXM.Tensai.Back.OKR.Domain.Entities
{
    public class Meeting
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime ScheduledTime { get; set; }
        public int Duration { get; set; } // Duration in minutes
        public string MeetingRoomUrl { get; set; }
        public string MeetingRoomToken { get; set; }
        
        // Relationships
        public string OrganizationId { get; set; }
        public int OrganizerId { get; set; } // User ID of the meeting creator
        public virtual ICollection<MeetingAttendee> Attendees { get; set; } = new List<MeetingAttendee>();
        
        // Status tracking
        public string Status { get; set; } // "scheduled", "in-progress", "completed", "canceled"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
} 