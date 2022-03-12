namespace Microdancer
{
    public interface IDrawable
    {
        bool Draw();
    }

    public interface IDrawable<T>
    {
        bool Draw(T content);
    }

    public interface IDrawable<T1, T2>
    {
        bool Draw(T1 content, T2 content2);
    }

    public interface IDrawable<T1, T2, T3>
    {
        bool Draw(T1 content, T2 content2, T3 content3);
    }

    public interface IDrawable<T1, T2, T3, T4>
    {
        bool Draw(T1 content, T2 content2, T3 content3, T4 content4);
    }
}
