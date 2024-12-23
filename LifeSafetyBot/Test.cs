using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace LifeSafetyBot
{
    public enum MedicalKit
    {
        Gloves = 0, Mask, Tourniquet, BandAid, IVL, Napkins, Scissors, SterileBandage, NonSterileBandage, None
    }

    public static class MedicalKitExtention
    {
        public static string GetDescription(this MedicalKit medicalKit)
        {
            return medicalKit switch
            {
                MedicalKit.Gloves => "Перчатки медицинские",
                MedicalKit.Mask => "Маска медицинская",
                MedicalKit.Tourniquet => "Жгут кровоостанавливающий",
                MedicalKit.BandAid => "Лейкопластырь бактериальный",
                MedicalKit.IVL => "Устройство для ИВЛ",
                MedicalKit.Napkins => "Салфетки марлевые стерильные",
                MedicalKit.Scissors => "Ножницы для разрезания повязок",
                MedicalKit.SterileBandage => "Бинт марлевый стерильный",
                MedicalKit.NonSterileBandage => "Бинт марлевый нестерильный",
                MedicalKit.None => null,
                _ => null,
            };
        }
        public static MedicalKit ToMedicalKit(this string str)
        {
            List<MedicalKit> kits = AllMedicalKits();
            for (int i = 0; i < kits.Count; i++)
            {
                if(str.ToLower() == kits[i].GetDescription().ToLower())
                {
                    return kits[i];
                }
            }
            return MedicalKit.None;
        }
        public static List<MedicalKit> AllMedicalKits()
        {
            int x = 0;
            var res = new List<MedicalKit>();
            while (((MedicalKit)x).GetDescription() != null)
            {
                res.Add(((MedicalKit)x));
                x++;
            }
            return res;
        }
    }

    public class Test
    {
        private const string _correct = "Успех!\nВы верно определили предметы медицинского назначения при оказании помощи пострадавшему!";
        private const string _incorrect = "Неудача!\nВы неверно определили предметы медицинского назначения при оказании помощи пострадавшему!";
        private readonly string _path;
        private List<List<MedicalKit>> _correctAnswers;
        private Queue<int> _randomList;
        private int _currentQuestionIndex;
        private List<string> _questions;
        private List<string> _wrongAnswers;
        private int _countOfCorrectAnswers;
        
        public int CountOfQuestions { get; }


        public Test()
        {
            _path = AppContext.BaseDirectory;
#if DEBUG
            _path = _path.Replace("\\bin\\Debug", "");
#else
            _path = _path.Replace("\\bin\\Release", "");
#endif
            _path += "TextFiles";

            if (!File.Exists(_path + "\\Questions.txt")) throw new NullReferenceException();
            _questions = File.ReadLines(_path + "\\Questions.txt").ToList();

            if (!File.Exists(_path + "\\WrongAnswers.txt")) throw new NullReferenceException();
            _wrongAnswers = File.ReadLines(_path + "\\WrongAnswers.txt").ToList();

            List<string> answers = new List<string>();
            if (!File.Exists(_path + "\\CorrectAnswers.txt"))
                throw new NullReferenceException();
            else
            {
                answers = File.ReadLines(_path + "\\CorrectAnswers.txt").ToList();
                _correctAnswers = new List<List<MedicalKit>>();

                foreach (var item in answers)
                {
                    _correctAnswers.Add(new List<MedicalKit>());
                    string s = new(item.Where(Char.IsDigit).ToArray());
                    for (int i = 0; i < s.Length; i++)
                    {
                        int x = s[i] - '0';
                        MedicalKit kit = (MedicalKit)x;
                        _correctAnswers.Last().Add(kit);
                    }
                }
            }

            CountOfQuestions = _questions.Count;
            _countOfCorrectAnswers = 0;

            List<int> random = new List<int>();
            for (int i = 0; i < CountOfQuestions; i++)
            {
                random.Add(i);
            }
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = random.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (byte.MaxValue / n)));
                int k = box[0] % n;
                n--;
                (random[n], random[k]) = (random[k], random[n]);
            }
            _randomList = new Queue<int>(random);

            _currentQuestionIndex = -1;

            
        }

        public string GetQuestion()
        {
            if (_randomList.Count <= 0)
            {
                _currentQuestionIndex = -1;
                return null;
            }
            _currentQuestionIndex = _randomList.Dequeue();
            return _questions[_currentQuestionIndex];
        }

        public string CheckAnswers(List<MedicalKit> userAnswers)
        {
            if (_currentQuestionIndex == -1) return null;
            if (userAnswers.Count == _correctAnswers[_currentQuestionIndex].Count)
            {
                foreach (var item in userAnswers)
                {
                    if (!_correctAnswers[_currentQuestionIndex].Contains(item)) return $"{_incorrect}\n{_wrongAnswers[_currentQuestionIndex]}";
                }
                _countOfCorrectAnswers++;
                return _correct;
            }
            return $"{_incorrect}\n{_wrongAnswers[_currentQuestionIndex]}";
        }

        public string GetResult()
        {
            return $"Вы набрали {_countOfCorrectAnswers}/{CountOfQuestions}!";
        }
    }
}
