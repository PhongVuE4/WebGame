using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common
{
    public enum ErrorCode
    {
        //room
        RoomAlreadyExists = 1001, // ten phong da ton tai
        RoomIdIsRequired = 1002, // yeu cau room id
        RoomIdNotFound = 1003, // roomid ko ton tai
        RoomPasswordRequired = 1004, // yeu cau password voi room co password
        RoomPasswordIsWrong = 1005,// sai password
        RoomRequiredHost = 1006,// yeu cau la host
        RoomModeException = 1007,// sai mode: party, friends, couples
        GameMustbePlaying = 1008,// game phai o trang thai playing
        RoomNotFoundPlayerIdException = 1009,// playerId ko o trong room
        QuestionTypeWrong = 1010,// sai type cua question: dare hoac type
        RoomEndStatusException = 1011,// game phai o trang thai playing
        RoomResetStatusException = 1012,//game phai o trang thai ended
        RoomAgeGroupException = 1013,// sai agegroup: kids, teen, adult, all
        RoomNameRequiredException = 1014,// yeu cau roomName
        RoomStartStatusException = 1015,// game chi bat dau khi o trang thai waiting
        RoomNotYourTurn = 1016,// chua toi luot
        RoomNextPlayerException = 1017,// chua toi luot hoac cau hoi da dc get
        RoomNoTimestampException = 1018,// lỗi gắn time để auto next player (đề phòng trường hợp afk)
        RoomNeedMoreTimeException = 1019,//doi 1s de next player
        NoActivePlayersException = 1020,// ko co player nao trong room



        //player
        PlayerNameLength = 2001, // do dai ten player
        FullPlayer = 2002, //phong full nguoi choi
        PlayerIdNotFound = 2003, // ko tim thay player id
        PlayerNameExisted = 2004, // ten player da ton tai
        PlayerNameRequiredException = 2005, // bat buoc co playerName
        PlayerIdCannotNull = 2006, // playerId ko dc null
        PlayerIdAlreadyInUseException = 2007, // playerId da ton tai voi ten khac trong phong

        //question
        QuestionTextRequired = 3001, // bat buoc nhap text question
        InvalidQuestionMode = 3002, // nhap dung mode friends, couples, party
        InvalidQuestionType = 3003, // nhap dung type truth, dare
        InvalidQuestionDifficulty = 3004, // nhap dung do kho east, medium, hard
        InvalidQuestionAgeGroup = 3005, // nhap dung do tuoi kids, adult, teen
        InvalidTimeLimit = 3006, // nhap timelimit hop le lon hon 0
        InvalidPoints = 3007, // nhap diem hop le lon hon 0
        QuestionAlreadyExists = 3008, // text question da ton tai
        QuestionNotFound = 3009, // ko co cau hoi nao
        EmptyQuestionList = 3010, // list cau hoi rong~
        MultipleValidationErrors = 3011, //bao loi cho truong hop add manyquestion

        //game session
        GameSessionRequired = 4001, // yeu cau gamesession id


        InvalidFilters = 9997, //filter ko hop le
        ValidationError = 9998, // loi validation
        InternalServerError = 9999 //500 Internal Server Error


    }
}
