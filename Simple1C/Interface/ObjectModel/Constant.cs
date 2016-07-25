namespace Simple1C.Interface.ObjectModel
{
    public abstract class Constant
    {
        public abstract object ЗначениеНетипизированное { get; set; }
    }

    public abstract class Constant<T> : Constant
    {
        public T Значение { get; set; }

        public override object ЗначениеНетипизированное
        {
            get { return Значение; }
            set { Значение = (T) value; }
        }
    }
}