namespace FormBuilderFinal.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsCurrentUser { get; set; }
    }
}
