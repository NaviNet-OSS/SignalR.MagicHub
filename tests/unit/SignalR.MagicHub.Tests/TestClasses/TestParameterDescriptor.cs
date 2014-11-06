using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

namespace SignalR.MagicHub.Test.TestClasses
{
    public class TestParameterDescriptor : HttpParameterDescriptor
    {
        private TestParameterDescriptor()
        {
            Configuration = new System.Web.Http.HttpConfiguration();
        }

        public static readonly HttpParameterDescriptor Default = new TestParameterDescriptor();

        public override string ParameterName
        {
            get { return "parameter"; }
        }

        public override Type ParameterType
        {
            get { return typeof(string); }
        }
    }

//    public class TestMetaDataprovider : System.Web.Http.Metadata.Providers.EmptyModelMetadataProvider
}
