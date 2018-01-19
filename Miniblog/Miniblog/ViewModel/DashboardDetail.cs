using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Miniblog.ViewModel
{
    public class DashboardDetail
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<string> CommentList { get; set; }
    }
}