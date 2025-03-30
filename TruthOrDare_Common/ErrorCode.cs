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
        RoomIdIsRequired = 1002,
        RoomIdNotFound = 1003, // roomid ko ton tai
        RoomPasswordRequired = 1004,
        RoomPasswordIsWrong = 1005,

        //player
        PlayerNameLength = 2001, // do dai ten player
        FullPlayer = 2002, //phong full nguoi choi
        PlayerIdNotFound = 2003, // ko tim thay player id
        PlayerNameExisted = 2004, // ten player da ton tai

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
        InvalidFilters = 3011, //filter ko hop le
        MultipleValidationErrors = 3012, //bao loi cho truong hop add manyquestion

        ValidationError = 9998, // loi validation
        InternalServerError = 9999 //500 Internal Server Error


    }
}
