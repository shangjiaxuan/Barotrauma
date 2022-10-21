using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using System.Collections;

using Barotrauma.Extensions;

namespace Barotrauma
{

    public interface IImplementsVariants<T> where T : Prefab
    {
        // direct parent of the prefab
        public PrefabInstance InheritParent => originalElement.InheritParent();

		// ancestry line of the prefab
		public IEnumerable<T> InheritHistory { 
            get {
                IImplementsVariants<T> cur = this;
                while(!(cur.InheritParent?.IsEmpty??true)){
                    if(cur.InheritParent.package.IsNullOrEmpty()){
                        cur = cur.GetPrevious(cur.InheritParent.id) as IImplementsVariants<T>;
					}
                    else{
                        cur = FindByPrefabInstance(cur.InheritParent) as IImplementsVariants<T>;
                    }
                    if (cur is null) break;
                    yield return cur as T;
				}
			} 
        }

        public XElement originalElement{ get; }

        public void InheritFrom(T parent);

        public bool CheckInheritHistory(T parent)
        {
            bool result = true;
			if ((parent as IImplementsVariants<T>).InheritHistory.Any(p => ReferenceEquals(p, this as T)))
		    {
				throw new Exception("Inheritance cycle detected: "
					+ string.Join(", ", InheritHistory.Select(n => "(id: " + n.Identifier.ToString() + ", package: " + n.ContentPackage!.Name + ")"),
					"(id: " + (this as T).Identifier.ToString() + ", package: " + (this as T).ContentPackage.Name + ")"));
			}
			return result;
        }

        public T FindByPrefabInstance(PrefabInstance instance);

        public T GetPrevious(Identifier id);

        public ContentXElement DoInherit(Action<ContentXElement, ContentXElement, ContentXElement> create_callback)
        {
            Stack<ContentXElement> preprocessed = new Stack<ContentXElement>();
            var last_elem = originalElement.FromContent((this as T).FilePath);
            foreach(var it in InheritHistory)
			{
				preprocessed.Push(last_elem.PreprocessInherit((it as IImplementsVariants<T>).originalElement.FromContent(it.ContentFile.Path), false));
				last_elem = preprocessed.Peek();
			}
            ContentXElement previous = preprocessed.Pop();
            while (preprocessed.Any())
            {
                previous = preprocessed.Pop().CreateVariantXML(previous, create_callback);
            }
            return originalElement.FromContent((this as T).ContentFile.Path).CreateVariantXML(previous, create_callback);
        }
    }

}
