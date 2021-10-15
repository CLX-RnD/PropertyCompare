using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace ConsoleApp3
{
    public class CompareAttribute : System.Attribute
    {

    }


    class Program
    {
        static void Main(string[] args)
        {
            AssetTemplate assetTemplate = new AssetTemplate()
            {
                ID = "1",
                Name = null,
                Comment = "Some comment",
                UpdatedOn = DateTime.MinValue,
                Data = new AssetTemplateData()
                {
                    Description = "Base Description",
                    AssetTags = new List<AssetTemplateAssetTagData>() { new AssetTemplateAssetTagData() { TagName = "Base Tag Name 1" }, new AssetTemplateAssetTagData() { TagName = null } }
                },
            };

            AssetTemplate newAssetTemplate = new AssetTemplate()
            {
                ID = "2",
                Name = "New Name",
                Comment = null,
                UpdatedOn = DateTime.Now,
                Data = new AssetTemplateData() { Description = "New Description",
                    AssetTags = new List<AssetTemplateAssetTagData>() { new AssetTemplateAssetTagData() { TagName = "New Tag Name 1" }, new AssetTemplateAssetTagData() { TagName = "New Tag Name 2" } } 
                },
            };

            Compare(assetTemplate, newAssetTemplate);

            Console.ReadKey();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseObject"></param>
        /// <param name="newObject"></param>
        static void Compare(object baseObject, object newObject)
        {
            // Store our types
            Type baseType = baseObject.GetType();
            Type newType = newObject.GetType();

            // If our types dont match, we should not be comparing with reflection
            if (baseType != newType)
                return;

#warning We are using the declared only binding flag to ignore the properties that C# adds onto things like "Lists". However this means inheritance cannot be compared

            //var allProperties = baseType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            // Loop through all properties that are public, instanced, and declared only
            foreach (var property in baseType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(property, typeof(CompareAttribute)))
                {
                    Console.WriteLine($"Comparing {property.Name}");

                    // Strings are returning as a char[], so we cannot just use value type
                    if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
                    {
                        var baseValue = property.GetValue(baseObject, null);
                        var newValue = property.GetValue(newObject, null);

                        if (!object.Equals(baseValue, newValue))
                        {
                            if (baseValue == null)
                                Console.WriteLine($"Set {newValue} to {property.Name}.");
                            else if (newValue == null)
                                Console.WriteLine($"Removed {baseValue} from {property.Name}.");
                            else
                                Console.WriteLine($"{property.Name} changed from {baseValue} to {newValue}");

                        }
                    }

                    // Check if we are a generic type. This typically means we have a list of data
                    else if (property.PropertyType.IsGenericType)
                    {
                        // For now, we do not want to process Lists
                        continue;

                        // If this is a list type, loop through the list and compare each object in the array
                        if (property.PropertyType.GetInterfaces().ToList().Where(it => it.Name == "IList").FirstOrDefault() != null)
                        {
                            var baseValue = property.GetValue(baseObject, null) as IEnumerable<object>;
                            var newValue = property.GetValue(newObject, null) as IEnumerable<object>;

                            // Check if the array is the same size or not
                            if (baseValue.Count() >= newValue.Count())
                            {
                                for (int count = 0; count < baseValue.Count(); count++)
                                {
                                    if (count < newValue.Count())
                                        Compare(baseValue.ElementAt(count), newValue.ElementAt(count));
                                    else  // There is 1 more value in the old array
                                        Compare(baseValue.ElementAt(count), null);
                                }
                            }
                            else if (newValue.Count() > baseValue.Count())
                            {
                                for (int count = 0; count < newValue.Count(); count++)
                                {
                                    if (count < baseValue.Count())
                                        Compare(baseValue.ElementAt(count), newValue.ElementAt(count));
                                    else // There is 1 more value in the new array
                                        Compare(null, newValue.ElementAt(count));
                                }
                            }
                        }
                        else
                        {
                            Console.Write($"The property type {property.PropertyType} is not supported.");
                        }
                    }

                    // If this is an object, then run compare on that object
                    else if (property.PropertyType.IsClass)
                    {
                        // For now, we do not want to process objects
                        continue;

                        var baseValue = property.GetValue(baseObject, null);
                        var newValue = property.GetValue(newObject, null);

                        Compare(baseValue, newValue);
                    }

                    else
                    {
                        Console.Write($"The property type {property.PropertyType} is not supported.");
                    }
                }
                else
                {
                    // Do nothing
                }
            }
        }
    }

    public class DataObject
    {
        [Compare]
        public string ID { get; set; }
    }


    public class AssetTemplate : DataObject
    {
        [Compare]
        public string Name { get; set; }

        [Compare]
        public string Comment { get; set; }

        public DateTime UpdatedOn { get; set; }

        public AssetTemplateData Data { get; set; }
    }

    public class AssetTemplateData
    { 
        public string Description { get; set; }

        public List<AssetTemplateAssetTagData> AssetTags { get; set; }
    }

    public class AssetTemplateAssetTagData
    {
        public string TagName { get; set; }
    }
}
