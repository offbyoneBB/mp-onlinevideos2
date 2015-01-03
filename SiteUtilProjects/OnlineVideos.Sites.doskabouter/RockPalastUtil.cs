using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class RockpalastUtil : GenericSiteUtil
    {

        public override int DiscoverSubCategories(Category parentCategory)
        {
            Regex sav = regEx_dynamicSubCategories;
            if (true.Equals(parentCategory.Other))
            {
                regEx_dynamicSubCategories = new Regex(@"<li\sclass=""teaserCont\s""\s>\s*<img\ssrc=""(?<thumb>[^""]*)""[^>]*>\s*<a\shref=""(?<url>[^""]*)""\sclass=""textPageLink"">(?<title>[^<]*)</a>\s*</li>", defaultRegexOptions);
            }

            int res = base.DiscoverSubCategories(parentCategory);
            if (parentCategory.Name == "Bands A - Z")
            {
                foreach (Category cat in parentCategory.SubCategories)
                {
                    cat.Other = true;
                    cat.HasSubCategories = true;
                }
            }
            if (true.Equals(parentCategory.Other))
            {
                regEx_dynamicSubCategories = sav;
            }
            return res;
        }
    }
}
