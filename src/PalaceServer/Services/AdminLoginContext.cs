namespace PalaceServer.Services
{
    public class AdminLoginContext
    {
        private readonly List<Guid> _tokenList;

        public AdminLoginContext()
        {
            _tokenList = new List<Guid>();
        }

        public void AddToken(Guid token)
        {
            if (_tokenList.Contains(token))
            {
                return;
            }
            _tokenList.Add(token);
        }

        public bool Contains(Guid token)
        {
            return _tokenList.Contains(token);
        }

        public void Remove(Guid token)
        {
            _tokenList.Remove(token);   
        }
    }
}
