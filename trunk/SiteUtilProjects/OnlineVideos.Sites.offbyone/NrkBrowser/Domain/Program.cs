/*
 * Created by: 
 * Created: 5. september 2008
 */

namespace Vattenmelon.Nrk.Domain
{
    public class Program : Item
    {
        public Program(string id, string title, string description, string bilde)
            : base(id, title)
        {
            Description = description;
            Bilde = bilde;
        }
    }
}