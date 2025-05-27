
namespace TNK.Core.BusinessAggregate.Specifications;

public class BusinessProfileByVendorIdSpec : Specification<BusinessProfile>, ISingleResultSpecification<BusinessProfile>
{
    public BusinessProfileByVendorIdSpec(string vendorId)
    {
        Query.Where(bp => bp.VendorId == vendorId);
    }
}
