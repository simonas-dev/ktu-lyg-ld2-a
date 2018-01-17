package xyz.simonas;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.util.ArrayList;
import java.util.List;

public class Main {
    private final File IN_FILE = new File("SankauskasS_L2a_dat.txt");

    private final int PROCESS_COUNT_N = 5;

    enum MainRunType {
        FULL,
        SEMI,
        NONE
    }

    class Item {
        String text;
        int numF;
        double dec;

        public Item(String line) {
            String[] split = line.split(" ");
            text = split[0];
            numF = Integer.valueOf(split[1]);
            dec = Double.valueOf(split[2]);
        }

    }

    class SortedItem {
        int field;
        int count;

        public SortedItem(int field, int count) {
            this.field = field;
            this.count = count;
        }

    }

    class ItemMonitor {
        boolean isLocked = false;
        Object lock = new Object();
        private List<SortedItem> sortedItemList = new ArrayList<>();
        int fullCount = 0;
        private int addedCount = 0;

        private int findIndex(Item item) {
            synchronized (lock) {
                for (SortedItem sortedItem : sortedItemList) {
                    if (sortedItem.field == item.numF) {
                        return this.sortedItemList.indexOf(sortedItem);
                    }
                }
                if (fullCount > addedCount) {
                    try {
                        lock.wait();
                    } catch (InterruptedException e) {
                        e.printStackTrace();
                    }
                    return findIndex(item);
                }
                return -1;
            }
        }

        private void removeOrDec(int index) {
            SortedItem sortedItem = sortedItemList.get(index);
            if (sortedItem.count > 1) {
                sortedItem.count -= 1;
//                System.out.println("dec. ");
            } else {
                sortedItemList.remove(index);
//                System.out.println("remove. ");
            }
        }

        public void removeItem(Item item) {
//            System.out.println("removeItem." + item.numF);
            synchronized (lock) {
                while (isLocked || addedCount == 0) {
                    System.out.println("wait.");
                    try {
                        lock.wait();
                    } catch (InterruptedException e) {
                        e.printStackTrace();
                    }
                }

                int removeAtIndex = findIndex(item);
                isLocked = true;
//                System.out.println("lock.");

                if (removeAtIndex != -1) {
                    System.out.println("rm i:" + removeAtIndex + " f:" + item.numF);
                    removeOrDec(removeAtIndex);
                } else {
                    System.out.println("rm not found " + item.numF);
                }
                isLocked = false;
                System.out.println("unlock.");
                lock.notifyAll();
            }
        }

        public void addSorted(Item item)  {
            System.out.println("addSorted. " + item.numF);
            synchronized (lock) {
                while (isLocked) {
                    System.out.println("wait.");
                    try {
                        lock.wait();
                    } catch (InterruptedException e) {
                        e.printStackTrace();
                    }
                }
                isLocked = true;
//                System.out.println("lock.");
                if (sortedItemList.size() == 0) {
                    sortedItemList.add(new SortedItem(item.numF, 1));
                    System.out.println("ad. " + item.numF + " c: 1");
                } else {
                    int originalSize = sortedItemList.size();
                    for (int i = 0; i < originalSize; i++) {
                        SortedItem curItem = sortedItemList.get(i);
                        if (curItem.field > item.numF) {
                            sortedItemList.add(i, new SortedItem(item.numF, 1));
                            System.out.println("ad. " + item.numF + " c: 1");
                            break;
                        } else if (curItem.field == item.numF) {
                            curItem.count += 1;
                            System.out.println("ad. " + item.numF + " c: " + curItem.count);
                            break;
                        } else if (i == originalSize - 1) {
                            sortedItemList.add(new SortedItem(item.numF, 1));
                            break;
                        }
                    }
                }
                addedCount += 1;
                isLocked = false;
                System.out.println("unlock.");
                lock.notifyAll();
            }
        }
    }

    public static void main(String[] args) throws IOException, InterruptedException {
//        new Main().start("1",MainRunType.FULL);
//        new Main().start("2", MainRunType.SEMI);
        new Main().start("3", MainRunType.NONE);
    }

    private void start(String ext, MainRunType type) throws IOException, InterruptedException {
        List<Item> data = readFile();
        List<List<Item>> dataChunks = getChunks(data);

        ItemMonitor sortedData = new ItemMonitor();

        List<Thread> threadList = new ArrayList<>();

        for (List<Item> dataChunk : dataChunks) {
            final List<Item> readChunk = new ArrayList<>();
            final List<Item> writeChunk = new ArrayList<>();

            switch (type) {
                case FULL:
                    readChunk.addAll(dataChunk);
                    sortedData.fullCount += readChunk.size();
                    writeChunk.addAll(dataChunk);
                    break;
                case NONE:
                    int size = dataChunk.size();
                    int pivot = size / 2;
                    readChunk.addAll(dataChunk.subList(0, pivot));
                    sortedData.fullCount += readChunk.size();
                    writeChunk.addAll(dataChunk.subList(pivot, size));
                    break;
                case SEMI:
                    readChunk.addAll(dataChunk);
                    sortedData.fullCount += readChunk.size();
                    writeChunk.addAll(readChunk);
                    writeChunk.remove(0);
                    writeChunk.remove(0);
                    break;
            }

            // Forming threads
            threadList.add(new Thread(() -> {
                for (Item item : readChunk) {
                    sortedData.addSorted(item);
                }
            }));

            // Modifier threads
            threadList.add(new Thread(() -> {
                for (Item item : writeChunk) {
                    sortedData.removeItem(item);
                }
            }));
        }

        for (Thread thread : threadList) {
            thread.start();
        }

        for (Thread thread : threadList) {
            thread.join();
        }

        System.out.println("sortedItemList: " + sortedData.sortedItemList.size());

        System.out.println("Done.");

        printTable(ext, sortedData.sortedItemList);
    }

    private void printTable(String ext, List<SortedItem> sortedItemList) throws IOException {
        List<String> lines = new ArrayList<>();
        lines.add("Field Count");
        for (SortedItem item : sortedItemList) {
            lines.add(item.field + " " + item.count);
        }
        Path path = new File("SankauskasS_L2a_" + ext + "_rez.txt").toPath();
        Files.write(path, lines, StandardOpenOption.CREATE);
    }

    private List<Item> readFile() throws IOException {
        List<Item> itemList = new ArrayList<>();
        List<String> lineList = Files.readAllLines(IN_FILE.toPath());
        for (String line: lineList) {
            itemList.add(new Item(line));
        }
        return itemList;
    }

    private List<List<Item>> getChunks(List<Item> lineList) {
        List<List<Item>> data = new ArrayList<>();

        int size = lineList.size();
        int limit = (int) Math.ceil(size / PROCESS_COUNT_N);
        int offset = 0;
        do {
            int nextOffset = offset + limit;
            nextOffset = nextOffset < size ? nextOffset : size;
            List<Item> chunkList = lineList.subList(offset, nextOffset);
            data.add(chunkList);
            offset += limit;
        } while (offset < size);

        return data;
    }

}
