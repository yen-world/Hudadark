using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using System.Text;
using System;

public class UserData {
    public string nickName;
    public string email;

    public override string ToString()
    {   
        StringBuilder result = new StringBuilder();
        result.AppendLine($"nickname : {nickName}");
        result.AppendLine($"email : {email}");
        return base.ToString();
    }

}


public class BackendGameData
{
    private static BackendGameData _instance = null;
    public static BackendGameData Instance {
        get { 
            if(_instance == null){
                _instance = new BackendGameData();
            }
            return _instance;
        }
    }

    public static UserData userData;

    private string gamedataRowInDate = String.Empty;

    public void GameDataInsert(string _nickname, string _email) {
        if(userData == null){
            userData = new UserData();
        }
        //user데이터 초기화
        userData.nickName = _nickname;
        userData.email = _email;

        Param param = new Param();
        param.Add("nickname", userData.nickName);
        param.Add("email", userData.email);

        var bro = Backend.GameData.Insert("USER_DATA", param);

        if(bro.IsSuccess()){
            gamedataRowInDate = bro.GetInDate();
        }
    }
    
    public UserData GameDataGet() {
        Debug.Log("게임 정보 불러오기");
        var bro = Backend.GameData.GetMyData("USER_DATA", new Where());
        if(bro.IsSuccess()){
            Debug.Log("게임 데이터 불러오기 성공! " + bro);

            //Json으로 리턴한 데이터 받아오기.
            LitJson.JsonData gameDataJson = bro.FlattenRows(); 
            if(gameDataJson.Count > 0){
                gamedataRowInDate = gameDataJson[0]["inDate"].ToString();

                userData = new UserData();
                userData.nickName = gameDataJson[0]["nickname"].ToString();
                userData.email = gameDataJson[0]["email"].ToString();
            }
            return userData;
        } else{
            //게임 데이터 조회 실패
            return null;
        }
    }

    public void GameDataUpdate() {
        if(userData == null){
            Debug.Log("서버에서 다운받거나 새로 삽입한 데이터가 존재하지 않습니다. Insert 혹은 Get을 통해 데이터를 생성해주세요.");
            return;
        }
        Param param = new Param();
        param.Add("nickname", userData.nickName);
        param.Add("email", userData.email);

        BackendReturnObject bro = null;
        if(string.IsNullOrEmpty(gamedataRowInDate)){
            //최신 데이터 수정
            bro = Backend.GameData.Update("USER_DATA", new Where(), param);
        } else { 
            //특정 데이터 수정
            bro = Backend.GameData.UpdateV2("USER_DATA",gamedataRowInDate, Backend.UserInDate, param);
        }
        //성공데이터는 bro.IsSuccess
    }
}
