using WebApi.Domain;

namespace WebApi.Mappers
{
    public interface IUserMapper
    {
        UserDetails FindByUserId(int userId);
        List<UserDetails> FindAll();
    }
}
