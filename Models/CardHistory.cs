// Path: Models/CardHistory.cs
using System;

namespace CardTagManager.Models
{
    public class CardHistory
    {
        public int Id { get; set; }
        public int CardId { get; set; }
        public Card Card { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; }
    }
}