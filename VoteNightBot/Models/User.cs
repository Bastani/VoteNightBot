using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VoteNightBot.Services;


namespace VoteNightBot.Models
{
    class User
    {
        [Key]
        public string ID { get; set; }
        public bool Voted { get; set; }
        public string MoviePickedId { get; set; }
    }
}
