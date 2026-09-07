using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 独立题库中心
/// 第一阶段：首题固定，剩余随机
/// 第二阶段：全部随机
/// </summary>
public static class QuizLibrary
{
    // 第一阶段【固定首题】每次答题第一道必定是此题
    private static Question Stage1FirstQuestion = new Question()
    {
        questionText = "这款游戏取材于《西游记》中的哪一故事情节？",
        optionA = "三打白骨精",
        optionB = "三借芭蕉扇",
        optionC = "大闹天宫",
        correctIndex = 0
    };

    /// <summary>第一阶段原始题库（除固定首题外的所有随机题目）</summary>
    private static List<Question> Stage1RandomPool()
    {
        List<Question> list = new List<Question>();
        #region 第一阶段题目
        list.Add(new Question()
        {
            questionText = "三打白骨精故事中，白骨精第一次变成了什么人？",
            optionA = "年轻村姑",
            optionB = "白发老妇人",
            optionC = "老公公",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "白骨精第二次幻化出来的人物是？",
            optionA = "村姑",
            optionB = "老妇人",
            optionC = "樵夫",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "白骨精第三次变化成什么前来欺骗唐僧？",
            optionA = "员外",
            optionB = "老妇人",
            optionC = "老公公",
            correctIndex = 2
        });
        list.Add(new Question()
        {
            questionText = "是谁火眼金睛识破白骨精的三次伪装？",
            optionA = "猪八戒",
            optionB = "孙悟空",
            optionC = "沙和尚",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "唐僧看不清妖怪伪装，主要因为他？",
            optionA = "没有火眼金睛",
            optionB = "眼睛失明",
            optionC = "故意包庇妖怪",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "白骨精居住的洞府叫什么名字？",
            optionA = "水帘洞",
            optionB = "白虎岭白骨洞",
            optionC = "盘丝洞",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "三打白骨精发生在哪一座山岭？",
            optionA = "火焰山",
            optionB = "白虎岭",
            optionC = "平顶山",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "白骨精第一次送饭给唐僧师徒，篮子里装的是什么？",
            optionA = "米饭馒头",
            optionB = "毒蛇、癞蛤蟆变的食物",
            optionC = "山果美酒",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "孙悟空第一次打死白骨精幻化的村姑后，唐僧的反应是？",
            optionA = "夸奖悟空除妖",
            optionB = "念紧箍咒惩罚孙悟空",
            optionC = "无动于衷",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "是谁在旁边搬弄是非，加剧唐僧对孙悟空的误会？",
            optionA = "沙和尚",
            optionB = "白龙马",
            optionC = "猪八戒",
            correctIndex = 2
        });
        list.Add(new Question()
        {
            questionText = "白骨精的本体是什么？",
            optionA = "千年狐狸",
            optionB = "一具白骨修炼成精",
            optionC = "蜘蛛精",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "孙悟空第三次打死白骨精之后，地上留下了什么证据？",
            optionA = "一把宝剑",
            optionB = "白骨，脊梁上刻白骨夫人",
            optionC = "金银珠宝",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "三打白骨精事件结局唐僧对孙悟空做了什么？",
            optionA = "奖赏孙悟空",
            optionB = "将孙悟空逐出师门赶走",
            optionC = "交给妖怪",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "孙悟空被赶走后回到了哪里？",
            optionA = "高老庄",
            optionB = "花果山",
            optionC = "流沙河",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "唐僧为什么执意赶走孙悟空？",
            optionA = "认为孙悟空滥杀无辜残害凡人",
            optionB = "悟空想吃唐僧肉",
            optionC = "悟空不想取经",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "白骨精的别称叫做？",
            optionA = "蝎子大王",
            optionB = "白骨夫人",
            optionC = "铁扇公主",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "白骨精的目的是什么？",
            optionA = "抢夺袈裟",
            optionB = "捉拿唐僧吃唐僧肉以求长生",
            optionC = "收徒弟",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "下面哪一位识破白骨精的伪装？",
            optionA = "孙悟空",
            optionB = "唐僧",
            optionC = "猪八戒",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "白骨精第三次伪装的老公公谎称来干什么？",
            optionA = "寻女儿和老伴",
            optionB = "化缘吃饭",
            optionC = "拜师取经",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "孙悟空要打第三次白骨精的时候，心中最大顾虑是？",
            optionA = "打不过妖怪",
            optionB = "怕唐僧再次误会自己",
            optionC = "想吃妖怪贡品",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "紧箍咒是谁传授给唐僧用来管束孙悟空的？",
            optionA = "玉皇大帝",
            optionB = "观音菩萨",
            optionC = "如来佛祖",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "三打白骨精一回，沙和尚的态度是？",
            optionA = "极力撺掇赶走悟空",
            optionB = "劝解唐僧，但话语权不大",
            optionC = "动手打孙悟空",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "白骨精有没有真正抓到过唐僧？",
            optionA = "抓到带走",
            optionB = "没有抓到，靠幻化骗人",
            optionC = "把唐僧关入洞府",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "孙悟空被驱逐时，向唐僧做了什么告别礼节？",
            optionA = "磕头拜别师父",
            optionB = "大骂唐僧",
            optionC = "直接转身就走",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "白骨精幻化的村姑谎称上山来干什么？",
            optionA = "烧香拜佛",
            optionB = "给丈夫送饭",
            optionC = "采药治病",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "白骨精变的老妇人谎称上山做什么？",
            optionA = "寻找女儿",
            optionB = "上山砍柴",
            optionC = "拜佛求子",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "《西游记》原著中三打白骨精属于哪个章节？",
            optionA = "尸魔三戏唐三藏 圣僧恨逐美猴王",
            optionB = "孙行者大闹黑风山",
            optionC = "三藏不忘本 四圣试禅心",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "白骨精属于什么类型妖怪，没有神仙坐骑背景？",
            optionA = "天上神仙下凡",
            optionB = "本土山野自行修炼妖魔",
            optionC = "菩萨的宠物",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "孙悟空离开唐僧之后，取经队伍接下来主要是谁保护唐僧？",
            optionA = "只有八戒沙僧白龙马",
            optionB = "天兵天将",
            optionC = "观音菩萨常驻身边",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "白骨精三次变化欺骗唐僧一共变化了几个人物形象？",
            optionA = "两个",
            optionB = "三个",
            optionC = "四个",
            correctIndex = 1
        });
        #endregion 
        return list;
    }

    /// <summary>组装第一阶段题库：首题固定 + 后续随机</summary>
    public static List<Question> GetStage1FixedAndRandomList(int needTotalCount)
    {
        List<Question> finalList = new List<Question>();

        // 1. 强制加入固定首题
        finalList.Add(Stage1FirstQuestion);

        // 2. 从随机题库抽取剩余数量
        int needRandomCount = needTotalCount - 1;
        if (needRandomCount > 0)
        {
            var randomQuestions = Stage1RandomPool()
                .OrderBy(x => Random.value)
                .Take(needRandomCount)
                .ToList();

            finalList.AddRange(randomQuestions);
        }

        return finalList;
    }

    private static List<Question> Stage1RandomPoo2()
    {
        List<Question> list = new List<Question>();
        #region 第二阶段题目
        list.Add(new Question()
        {
            questionText = "《西游记》的作者是谁？",
            optionA = "吴承恩",
            optionB = "罗贯中",
            optionC = "施耐庵",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "孙悟空的出生地是哪里？",
            optionA = "峨眉山",
            optionB = "花果山",
            optionC = "普陀山",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "孙悟空的兵器叫什么名字？",
            optionA = "方天画戟",
            optionB = "青龙偃月刀",
            optionC = "如意金箍棒",
            correctIndex = 2
        });
        list.Add(new Question()
        {
            questionText = "孙悟空被压在哪座山下五百年？",
            optionA = "五行山",
            optionB = "泰山",
            optionC = "华山",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "唐僧的俗家姓什么？",
            optionA = "李",
            optionB = "陈",
            optionC = "王",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "猪八戒原本是什么天庭官职？",
            optionA = "托塔天王",
            optionB = "卷帘大将",
            optionC = "天蓬元帅",
            correctIndex = 2
        });
        list.Add(new Question()
        {
            questionText = "沙和尚在天庭原来的职位是？",
            optionA = "卷帘大将",
            optionB = "天蓬元帅",
            optionC = "二十八星宿",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "白龙马原本是什么身份？",
            optionA = "东海龙王太子",
            optionB = "西海龙王三太子",
            optionC = "南海龙王太子",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "唐僧收孙悟空是在哪个地点？",
            optionA = "五行山",
            optionB = "高老庄",
            optionC = "流沙河",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "唐僧在哪里收服猪八戒？",
            optionA = "流沙河",
            optionB = "高老庄",
            optionC = "火焰山",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "唐僧收服沙和尚的地方是？",
            optionA = "流沙河",
            optionB = "通天河",
            optionC = "黑水河",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "观音菩萨给唐僧用来管束孙悟空的宝物是？",
            optionA = "金刚琢",
            optionB = "紧箍咒",
            optionC = "紫金铃",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "大闹天宫时，孙悟空和谁打赌输了被压五行山？",
            optionA = "玉皇大帝",
            optionB = "如来佛祖",
            optionC = "太上老君",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "三打白骨精中，白骨精第三次变成什么？",
            optionA = "老翁",
            optionB = "老妇人",
            optionC = "年轻村姑",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "火焰山的火是谁放出来的？",
            optionA = "红孩儿",
            optionB = "孙悟空",
            optionC = "牛魔王",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "想要熄灭火焰山的火需要借谁的芭蕉扇？",
            optionA = "铁扇公主",
            optionB = "观音菩萨",
            optionC = "王母娘娘",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "红孩儿的绝招是什么？",
            optionA = "寒冰掌",
            optionB = "三昧真火",
            optionC = "吐毒雾",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "是谁收服了红孩儿？",
            optionA = "如来佛祖",
            optionB = "观音菩萨",
            optionC = "太上老君",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "真假美猴王中，假孙悟空是什么妖怪？",
            optionA = "六耳猕猴",
            optionB = "通臂猿猴",
            optionC = "石猴精",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "唐僧师徒一共经历多少难取得真经？",
            optionA = "七十二难",
            optionB = "八十一难",
            optionC = "九十九难",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "《西游记》中，唐僧取经终点是哪里？",
            optionA = "灵山雷音寺",
            optionB = "凌霄宝殿",
            optionC = "普陀珞珈山",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "偷吃人参果故事发生在哪个道观？",
            optionA = "三清观",
            optionB = "五庄观",
            optionC = "玄都观",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "五庄观的观主镇元子和谁是结拜兄弟？",
            optionA = "唐僧",
            optionB = "孙悟空",
            optionC = "观音菩萨",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "金角大王银角大王是谁的童子下凡？",
            optionA = "太上老君",
            optionB = "观音菩萨",
            optionC = "弥勒佛",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "女儿国国王想招谁做驸马？",
            optionA = "孙悟空",
            optionB = "猪八戒",
            optionC = "唐僧",
            correctIndex = 2
        });
        list.Add(new Question()
        {
            questionText = "盘丝洞的妖怪是什么精？",
            optionA = "蜘蛛精",
            optionB = "蝎子精",
            optionC = "老鼠精",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "狮驼岭三大魔王中，大鹏金翅雕是谁的亲戚？",
            optionA = "玉皇大帝",
            optionB = "如来佛祖",
            optionC = "元始天尊",
            correctIndex = 1
        });
        list.Add(new Question()
        {
            questionText = "取得真经后孙悟空被封为什么？",
            optionA = "斗战胜佛",
            optionB = "金身罗汉",
            optionC = "旃檀功德佛",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "取得真经后猪八戒被封为什么？",
            optionA = "净坛使者",
            optionB = "金身罗汉",
            optionC = "八部天龙",
            correctIndex = 0
        });
        list.Add(new Question()
        {
            questionText = "取得真经后沙和尚被封为什么？",
            optionA = "净坛使者",
            optionB = "金身罗汉",
            optionC = "斗战胜佛",
            correctIndex = 1
        });
        #endregion 

        return list;
    }
    /// <summary>第二阶段题库（完全随机，无固定题）</summary>
    public static List<Question> Stage2QuizList(int needTotalCount)
    {
        List<Question> finalList1 = new List<Question>();

        int needRandomCount = needTotalCount;
        if (needRandomCount > 0)
        {
            var randomQuestions = Stage1RandomPoo2()
                .OrderBy(x => Random.value)
                .Take(needRandomCount)
                .ToList();

            finalList1.AddRange(randomQuestions);
        }

        return finalList1;
    }
}
