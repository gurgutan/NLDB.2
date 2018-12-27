namespace NLDB
{
    public class DValue
    {
        public int count;
        public float sum;

        public DValue(int count, float sum)
        {
            this.count = count;
            this.sum = sum;
        }

        public float Average() => this.sum / this.count;

    }
}