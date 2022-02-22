namespace Microdancer
{
    public interface IDrawable
    {
        void Draw();
    }

    public interface IDrawable<T>
    {
        void Draw(T content);
    }
}
