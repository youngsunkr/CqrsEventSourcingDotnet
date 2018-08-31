using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CqrsEventSourcingDotnet
{
    // https://www.youtube.com/watch?v=Q0Bz-O67_nI&t=76s

    // CQRS = command query responsibility segregation
    // CQS = command query separation

    // COMMAND = do/change

    public class PersonStorage
    {
        Dictionary<int, Person> people;

    }
    public class Person
    {
        public int UniqueId;
        private int age;
        EventBroker broker;

        public Person(EventBroker broker)
        {
            this.broker = broker;
            broker.Commands += Broker_Commands;
            broker.Queries += Broker_Queries;
        }

        private void Broker_Queries(object sender, Query query)
        {
            var ac = query as AgeQuery;
            if (ac != null && ac.Target == this)
            {
                ac.Result = age;
            }
        }

        private void Broker_Commands(object sender, Command command)
        {
            var cac = command as ChangeAgeCommand;
            if (cac != null && cac.Target == this)
            {
                if (cac.Register) broker.AllEvents.Add(new AgeChangedEvent(this, age, cac.Age));
                age = cac.Age;
            }
        }

        public bool CanVote => age >= 16;
    }

    public class EventBroker
    {
        // 1. All event the happened.
        public IList<Event> AllEvents = new List<Event>();
        // 2. Command
        public event EventHandler<Command> Commands;
        // 3. Query
        public event EventHandler<Query> Queries;

        public void Command(Command c)
        {
            Commands?.Invoke(this, c);
        }

        public T Query<T>(Query q)
        {
            Queries?.Invoke(this, q);
            return (T)q.Result;
        }

        public void UndoLast()
        {
            var e = AllEvents.LastOrDefault();
            var ac = e as AgeChangedEvent;
            if (ac != null)
            {
                Command(new ChangeAgeCommand(ac.Targe, ac.Oldvalue) { Register = false });
                AllEvents.Remove(e);
            }
        }

    }

    public class Query
    {
        public object Result;
    }

    class AgeQuery : Query
    {
        public Person Target;
    }

    public class Command : EventArgs
    {
        public bool Register = true;
    }

    class ChangeAgeCommand : Command
    {
        public Person Target;
        public int TargetId;
        public int Age;

        public ChangeAgeCommand(Person target, int age)
        {
            Target = target;
            Age = age;
        }
    }

    public class Event
    {
        //backtrack

    }

    class AgeChangedEvent : Event
    {
        public Person Targe;
        public int Oldvalue, Newvalue;

        public AgeChangedEvent(Person target, int oldvalue, int newvalue)
        {
            Targe = target;
            Oldvalue = oldvalue;
            Newvalue = newvalue;
        }

        public override string ToString()
        {
            return $"Age changed from {Oldvalue} to {Newvalue}";
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var eb = new EventBroker();
            var p = new Person(eb);
            eb.Command(new ChangeAgeCommand(p, 123));

            foreach (var e in eb.AllEvents)
            {
                Console.WriteLine(e);
            }

            int age;
            age = eb.Query<int>(new AgeQuery { Target = p });
            Console.WriteLine(age);

            eb.UndoLast();
            foreach (var e in eb.AllEvents)
            {
                Console.WriteLine(e);
            }

            age = eb.Query<int>(new AgeQuery { Target = p });
            Console.WriteLine(age);


            Console.ReadKey();
        }
    }
}
