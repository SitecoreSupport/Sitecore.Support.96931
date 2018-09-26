namespace Sitecore.Support.XA.Foundation.VersionSpecific
{
  using Sitecore.ContentSearch;
  using Sitecore.ContentSearch.ComputedFields;
  using Sitecore.Data.LanguageFallback;
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using Sitecore.ContentSearch.Diagnostics;

  public class CustomSolrDocumentBuilder : Sitecore.XA.Foundation.VersionSpecific.CustomSolrDocumentBuilder
  {
    public CustomSolrDocumentBuilder(IIndexable indexable, IProviderUpdateContext context)
        : base(indexable, context)
    {
    }
    
  }
}