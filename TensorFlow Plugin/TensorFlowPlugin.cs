using Python.Runtime;

namespace bezlio.rdb.plugins
{
    public class TensorFlowPlugin
    {
        public static string TF_HelloWorld()
        {
            using (Py.GIL())
            {
                dynamic tf = Py.Import("tensorflow");
                dynamic hello = tf.constant("Hello, TensorFlow!");
                dynamic sess = tf.Session();
                return sess.run(hello);
            }
        }
    }
}
