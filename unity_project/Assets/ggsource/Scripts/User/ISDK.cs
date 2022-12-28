using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISDK
{
    void Init();
    void Login();
    void Logout();
    void Quit();
    void Pay(UserSystem.Order order, UserSystem.UserData userData=null);
    void SubmitData(string jsonData);
}
