namespace Sitecore.Support.ContentSearch.LuceneProvider
{
  using Sitecore.ContentSearch;
  using Sitecore.ContentSearch.ComputedFields;
  using Sitecore.ContentSearch.Diagnostics;
  using Sitecore.Data.LanguageFallback;
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Threading.Tasks;

  public class LuceneDocumentBuilder : Sitecore.ContentSearch.LuceneProvider.LuceneDocumentBuilder
  {
    public LuceneDocumentBuilder(IIndexable indexable, IProviderUpdateContext context)
        : base(indexable, context)
    {
    }
    
  }
}