namespace JailerGame.Identity
{
    /// <summary>
    /// 玩家身份。Informer = 普通告密者，Emissary = 使者（卧底）。
    /// 这是隐藏信息：每个客户端只知道自己的身份，
    /// 联机时由权威服务器在开局随机分配后只下发本人身份。
    /// </summary>
    public enum PlayerIdentity
    {
        Informer = 0,  // 告密者（普通玩家）
        Emissary = 1,  // 使者（卧底，1局1人）
    }
}
