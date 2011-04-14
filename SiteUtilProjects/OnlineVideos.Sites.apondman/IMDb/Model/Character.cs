using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    public class Character
    {
        public string Name { get; internal set; }

        public NameReference Actor { get; internal set; }

    }
}
