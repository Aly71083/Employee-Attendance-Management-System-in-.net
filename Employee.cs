using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATA
{
   
        [Table("employee")]
        public class Employee
        {
            [Key]
            public int Id { get; set; }

            [Required(ErrorMessage = "Name is required")]
            [StringLength(100)]
            public required string Name { get; set; }

            [Required(ErrorMessage = "Department is required")]
            [StringLength(50)]
            public required string Department { get; set; }

            [Required(ErrorMessage = "Join Date is required")]
            public DateTime JoinDate { get; set; }
        
    }
}
