using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace NDDigital.Component.Core.Util.Dynamics
{
    public delegate bool IsFiltered(dynamic element, object tag);

    public class DynamicXmlObject : DynamicObject, IDisposable
    {
        private XElement _root;
        private XAttribute _attRoot;
        private XNamespace _xmlns = string.Empty;
        private List<DynamicElement> _elements = new List<DynamicElement>();
        private SortedList<string, List<DynamicXmlObject>> _multipleElements = new SortedList<string, List<DynamicXmlObject>>();

        public string LocalName
        {
            get
            {
                if (_root == null)
                    return null;
                return _root.Name.LocalName;
            }
        }

        public XNamespace Xmlns
        {
            get { return _xmlns; }
            set { _xmlns = value; }
        }

        private DynamicXmlObject(XElement root)
        {
            _root = root;
            var xmlns = root.Attribute("xmlns");
            _xmlns = xmlns != null ? xmlns.Value : string.Empty;
        }

        private DynamicXmlObject(XElement root, XNamespace xmlns)
        {
            _root = root;
            _xmlns = xmlns;
        }

        private DynamicXmlObject(XAttribute root)
        {
            _attRoot = root;
        }

        public static DynamicXmlObject Parse(string xmlString)
        {
            return new DynamicXmlObject(XDocument.Parse(xmlString).Root);
        }

        public static DynamicXmlObject Load(string filename)
        {
            return new DynamicXmlObject(XDocument.Load(filename).Root);
        }

        private DynamicXmlObject GetDynamic(XObject xobj)
        {
            DynamicElement element = _elements.FirstOrDefault(p => p.Node == xobj);
            if(element == null)
            {
                element = new DynamicElement();
                element.Node = xobj;
                if(xobj is XAttribute)
                    element.Dynamic = new DynamicXmlObject((XAttribute)xobj);
                else
                    element.Dynamic = new DynamicXmlObject((XElement)xobj, _xmlns);
                _elements.Add(element);
            }
            return element.Dynamic;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            var att = _root.Attribute(binder.Name);
            if(att != null)
            {
                result = GetDynamic(att);
                return true;
            }

            string binderName = _xmlns.NamespaceName + binder.Name;
            List<DynamicXmlObject> multiples;
            if(_multipleElements.TryGetValue(binderName, out multiples))
            {
                result = multiples;
                return true;
            }
            else
            {
                var nodes = _root.Elements(_xmlns + binder.Name);
                if(nodes.Count() > 1)
                {
                    result = nodes.Select(n => GetDynamic(n)).ToList();
                    _multipleElements.Add(binderName, result as List<DynamicXmlObject>);
                    return true;
                }
            }

            var node = _root.Element(_xmlns + binder.Name);
            if(node != null)
            {
                result = GetDynamic(node);
                return true;
            }
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            string value = (_root != null) ? _root.Value : _attRoot.Value;
            if(binder.Type == typeof(string))
            {
                result = value;
                return true;
            }

            var parseMethod = binder.Type.GetMethod("Parse", new Type[] { typeof(string) });
            if(parseMethod != null)
            {
                result = parseMethod.Invoke(null, new object[] { value });
                return true;
            }
            else if(binder.Type.IsEnum)
            {
                result = Enum.Parse(binder.Type, value);
                return true;
            }


            return base.TryConvert(binder, out result);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if(base.TryGetIndex(binder, indexes, out result) == false)
            {
                result = GetDynamic(_root);
                if(result == null)
                    return false;
            }
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if(_root == null)
                return false;
            var node = _root.Element(_xmlns + binder.Name);
            if(value == null && node != null)
            {
                node.Remove();
                return true;
            }
            if(node == null)
                _root.Add(new XElement(_xmlns + binder.Name, value));
            else
                node.SetValue(value);
            return true;
        }

        public int Count
        {
            get
            {
                var parent = _root.Parent;
                if(parent != null)
                    return parent.Elements(_root.Name).Count();
                return 0;
            }
        }

        public bool IsEqual(object toCompare)
        {
            if(toCompare is DynamicXmlObject)
            {
                DynamicXmlObject tc = (DynamicXmlObject)toCompare;
                return _root.Value.Equals(tc._root.Value);
            }
            return _root.Value.Equals(toCompare);
        }

        public bool FindInSameElements(DynamicXmlObject toFindIn)
        {
            var nodes = toFindIn._root.Elements(_root.Name);
            string rootValue = _root.Value;
            foreach(var node in nodes)
                if(rootValue.Equals(node.Value))
                    return true;
            return false;
        }

        public string Value
        {
            get
            {
                return (_root == null) ? null : _root.Value;
            }
        }

        public DynamicXmlObject[] GetElements(string childName, IsFiltered filter = null, object tag = null)
        {
            var nodes = _root.Elements(_xmlns + childName);
            if(nodes.Count() > 0)
            {
                List<DynamicXmlObject> result = nodes.Select(n => GetDynamic(n)).ToList();
                if(filter != null)
                {
                    List<DynamicXmlObject> filteredResult = new List<DynamicXmlObject>();
                    foreach(var element in result)
                    {
                        if(filter(element, tag))
                            filteredResult.Add(element);
                    }
                    result = filteredResult;
                }
                return result.ToArray();
            }
            return new DynamicXmlObject[0];
        }

        public DynamicXmlObject GetFirstElement(string childName, IsFiltered filter = null, object tag = null)
        {
            var nodes = _root.Elements(_xmlns + childName);
            if(nodes.Count() > 0)
            {
                List<DynamicXmlObject> result = nodes.Select(n => GetDynamic(n)).ToList();
                if(filter != null)
                {
                    List<DynamicXmlObject> filteredResult = new List<DynamicXmlObject>();
                    foreach(var element in result)
                    {
                        if(filter(element, tag))
                            filteredResult.Add(element);
                    }
                    result = filteredResult;
                }
                return result.FirstOrDefault();
            }
            return null;
        }


        public override string ToString()
        {
            return (_root == null) ? base.ToString() : _root.ToString();
        }

        public void Dispose()
        {

            _root = null;
            _attRoot = null;
            _xmlns = null;
            if(_elements != null)
            {
                foreach(var element in _elements)
                    element.Dynamic.Dispose();
                _elements.Clear();
                _elements = null;
            }
            if(_multipleElements != null)
            {
                foreach(var multElement in _multipleElements)
                    foreach(var element in multElement.Value)
                        element.Dispose();
                _multipleElements.Clear();
                _multipleElements = null;
            }
        }
    }

    internal class DynamicElement
    {
        public XObject Node { get; set; }
        public DynamicXmlObject Dynamic { get; set; }
    }
}
