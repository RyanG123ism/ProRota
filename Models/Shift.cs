﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProRota.Models
{
    public class Shift
    {

        public int Id { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public DateTime? StartDateTime { get; set; } = DateTime.MinValue;

        [Required]
        [Display(Name = "End Time")]
        public DateTime? EndDateTime { get; set; } = DateTime.MinValue;

        [DataType(DataType.Text)]
        public string? ShiftNotes { get; set; } = string.Empty; //eg. bar today / break cover

        //navigational properties

        [ForeignKey("ApplicationUserId")]
        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }

        [ForeignKey("SiteId")]
        public int? SiteId { get; set; }
        public Site? Site { get; set; }




    }
}
